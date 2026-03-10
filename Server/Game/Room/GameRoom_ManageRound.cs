using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private readonly int _upkeepParam = 4;

    private readonly struct GameOverContext(int roomId, GameMode mode, int stageId,
        int winnerUserId, int loserUserId, int winnerPlayerId, int loserPlayerId, 
        int winnerRankPoint, int winnerWinRankPoint, int loserRankPoint, int loserLoseRankPoint,
        Faction winnerFaction, int singleStar, bool winnerIsHuman)
    {
        public readonly int RoomId = roomId;
        public readonly GameMode Mode = mode;
        public readonly int StageId = stageId;

        public readonly int WinnerUserId = winnerUserId;
        public readonly int LoserUserId = loserUserId;

        public readonly int WinnerPlayerId = winnerPlayerId;
        public readonly int LoserPlayerId = loserPlayerId;

        // Rank(룸 상태에서 숫자만 복사)
        public readonly int WinnerRankPoint = winnerRankPoint;
        public readonly int WinnerWinRankPoint = winnerWinRankPoint;
        public readonly int LoserRankPoint = loserRankPoint;
        public readonly int LoserLoseRankPoint = loserLoseRankPoint;

        // Single/Tutorial
        public readonly Faction WinnerFaction = winnerFaction;
        public readonly int SingleStar = singleStar;         // 싱글이면 1~3, 아니면 0
        public readonly bool WinnerIsHuman = winnerIsHuman;     // 싱글에서 winner가 실제 승자인지
    }
    
    private struct GameOverResult
    {
        // Rank
        public PlayerRewardPackets RewardPackets;

        // Single
        public S_ShowSingleResultPopup? SinglePacketWinner;
        public S_ShowSingleResultPopup? SinglePacketLoser;

        // Tutorial
        public S_SendTutorialReward? TutorialPacket;

        // Friendly
        public S_ShowFriendlyResultPopup? FriendlyPacketWinner;
        public S_ShowFriendlyResultPopup? FriendlyPacketLoser;

        // 외부 이벤트는 I/O에서 처리해도 되고, 요청만 담아 Commit에서 fire-and-forget 해도 됨
        public List<(List<int> UserIds, string EventKey, EventCounterKey CounterKey)> EventRequests;
    }
    
    private struct PlayerRewardPackets
    {
        public S_ShowRankResultPopup WinnerPacket;
        public S_ShowRankResultPopup LoserPacket;
    }

    public void HandleSurrender(int loserUserId)
    {
        if (_gameOverState != GameOverState.Running) return;
        int winnerUserId = _players.Values.Where(p => p.Session?.UserId != loserUserId)
            .OrderByDescending(p => p.Session?.SessionId)
            .FirstOrDefault()?.Session?.UserId ?? -1;
        
        _gameOverState = GameOverState.Pending;
        var context = BuildGameOverContext(winnerUserId, loserUserId);
        _ = Task.Run(async () =>
        {
            GameOverResult result = default;
            try
            {
                result = await FetchGameOverDataAsync(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Push(CommitGameOver, context, result);
        });
    }
    
    private void CheckState()
    {
        if (_gameOverState != GameOverState.Running) return;
        if (Round > 0 && TryDecideWinner(out int winnerUserId, out int loserUserId))
        {
            _gameOverState = GameOverState.Pending;
            
            // 룸 컨텍스트에서 스냅샷 생성 - 룸 상태 접근은 여기서만
            GameOverContext context = BuildGameOverContext(winnerUserId, loserUserId);
            
            // I/O 작업은 백그라운드에서만, 룸 상태/세션 접근 금지
            _ = Task.Run(async () =>
            {
                GameOverResult result = default;
                try
                {
                    result = await FetchGameOverDataAsync(context);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Room {RoomId}] GameOver error: {e}");
                }
                finally
                {
                    Push(CommitGameOver, context, result);
                }
            });

            return;
        }
        
        TrackUnits();
    }

    private GameOverContext BuildGameOverContext(int winnerUserId, int loserUserId)
    {
        var loserPlayer = _players.Values
            .Where(p => p.Session?.UserId == loserUserId)
            .OrderByDescending(p => p.Session?.SessionId)
            .FirstOrDefault();

        var winnerPlayer = _players.Values
            .Where(p => p.Session?.UserId == winnerUserId)
            .OrderByDescending(p => p.Session?.SessionId)
            .FirstOrDefault();

        int winnerPlayerId = winnerPlayer?.Id ?? 0;
        int loserPlayerId = loserPlayer?.Id ?? 0;

        int winnerRankPoint = winnerPlayer?.RankPoint ?? 0;
        int winnerWinRankPoint = winnerPlayer?.WinRankPoint ?? 0;
        int loserRankPoint = loserPlayer?.RankPoint ?? 0;
        int loserLoseRankPoint = loserPlayer?.LoseRankPoint ?? 0;

        Faction winnerFaction = winnerPlayer?.Faction ?? Faction.Sheep;

        int singleStar = 0;
        bool winnerIsHuman = true;

        if (GameMode == GameMode.Single)
        {
            if (winnerPlayer == null)
            {
                winnerIsHuman = false;
            }
            else
            {
                singleStar = CheckStars(winnerPlayer.Faction);
                winnerIsHuman = true;
            }
        }

        return new GameOverContext(
            RoomId, GameMode, StageId,
            winnerUserId, loserUserId,
            winnerPlayerId, loserPlayerId,
            winnerRankPoint, winnerWinRankPoint,
            loserRankPoint, loserLoseRankPoint,
            winnerFaction,
            singleStar,
            winnerIsHuman
        );
    }

    private async Task<GameOverResult> FetchGameOverDataAsync(GameOverContext context)
    {
        var result = new GameOverResult { EventRequests = [] };

        switch (context.Mode)
        {
            case GameMode.Test:
            {
                result.RewardPackets = new PlayerRewardPackets
                {
                    LoserPacket = new S_ShowRankResultPopup
                    {
                        Win = false, RankPointValue = 0, RankPoint = context.LoserRankPoint
                    },
                    WinnerPacket = new S_ShowRankResultPopup
                    {
                        Win = true, RankPointValue = 0, RankPoint = context.WinnerRankPoint
                    }
                };
                break;
            }

            case GameMode.Rank:
            {
                var required = new RankGameRewardPacketRequired
                {
                    WinUserId = context.WinnerUserId,
                    WinRankPoint = context.WinnerWinRankPoint,
                    LoseUserId = context.LoserUserId,
                    LoseRankPoint = context.LoserLoseRankPoint
                };
                
                var res = await NetworkManager.Instance.SendRequestToApiAsync<RankGameRewardPacketResponse>(
                    "Match/RankGameReward", required, HttpMethod.Put);

                var loserPacket = new S_ShowRankResultPopup
                {
                    Win = false,
                    RankPointValue = context.LoserLoseRankPoint,
                    RankPoint = context.LoserRankPoint
                };

                var winnerPacket = new S_ShowRankResultPopup
                {
                    Win = true,
                    RankPointValue = context.WinnerWinRankPoint,
                    RankPoint = context.WinnerRankPoint
                };

                if (res != null)
                {
                    foreach (var r in res.LoserRewards)
                        loserPacket.Rewards.Add(new Reward
                        {
                            ItemId = r.ItemId, ProductType = r.ProductType, Count = r.Count
                        });

                    foreach (var r in res.WinnerRewards)
                        winnerPacket.Rewards.Add(new Reward
                        {
                            ItemId = r.ItemId, ProductType = r.ProductType, Count = r.Count
                        });
                }

                result.RewardPackets = new PlayerRewardPackets { WinnerPacket = winnerPacket, LoserPacket = loserPacket };
                break;
            }
            
            case GameMode.Single:
            {
                if (context.WinnerIsHuman)
                {
                    var req = new SingleGameRewardPacketRequired
                    {
                        UserId = context.WinnerUserId,
                        StageId = context.StageId,
                        Star = context.SingleStar
                    };

                    var res = await NetworkManager.Instance.SendRequestToApiAsync<SingleGameRewardPacketResponse>(
                        "Match/SingleGameReward", req, HttpMethod.Put);

                    var packet = new S_ShowSingleResultPopup { Win = true, Star = context.SingleStar };
                    if (res != null)
                    {
                        foreach (var ri in res.Rewards)
                        {
                            packet.SingleRewards.Add(new SingleReward
                            {
                                ItemId = ri.ItemId,
                                ProductType = ri.ProductType,
                                Count = ri.Count,
                                Star = ri.Star
                            });
                        }
                    }

                    result.SinglePacketWinner = packet;

                    // 이벤트는 I/O에서 처리 (원하면 Commit에서 fire-and-forget로 해도 됨)
                    await UserEventManager.Instance.EventProgressHandler(
                        new List<int> { context.WinnerUserId },
                        context.RoomId,
                        "single_play_clear_2026",
                        EventCounterKey.single_play_win);
                }
                else
                {
                    result.SinglePacketLoser = new S_ShowSingleResultPopup { Win = false };
                }

                break;
            }

            case GameMode.Tutorial:
            {
                var req = new TutorialRewardPacketRequired
                {
                    UserId = context.WinnerUserId,
                    Faction = context.WinnerFaction
                };

                var res = await NetworkManager.Instance.SendRequestToApiAsync<TutorialRewardPacketResponse>(
                    "Match/TutorialReward", req, HttpMethod.Put);

                var packet = new S_SendTutorialReward
                {
                    RewardUnitId = res == null ? UnitId.UnknownUnit : (UnitId)res.Rewards.First().ItemId
                };

                result.TutorialPacket = packet;
                break;
            }

            case GameMode.Friendly:
            {
                result.FriendlyPacketLoser = new S_ShowFriendlyResultPopup { Win = false };
                result.FriendlyPacketWinner = new S_ShowFriendlyResultPopup { Win = true };

                await UserEventManager.Instance.EventProgressHandler(
                    new List<int> { context.LoserUserId, context.WinnerUserId },
                    context.RoomId,
                    "friendly_match_2026",
                    EventCounterKey.friendly_match);

                break;
            }
        }

        return result;
    }
    
    private void CommitGameOver(GameOverContext context, GameOverResult result)
    {
        if (_gameOverState == GameOverState.Committed) return;
        if (_gameOverState != GameOverState.Pending) return;

        _gameOverState = GameOverState.Committed;
        // 안전하게 플레이어 다시 찾기(스냅샷의 PlayerId 사용)
        _players.TryGetValue(context.WinnerPlayerId, out var winnerPlayer);
        _players.TryGetValue(context.LoserPlayerId, out var loserPlayer);

        switch (context.Mode)
        {
            case GameMode.Test:
            {
                if (loserPlayer != null)
                {
                    loserPlayer.Session?.Send(result.RewardPackets.LoserPacket);
                    LeaveGame(loserPlayer.Id);
                }

                if (winnerPlayer != null)
                {
                    winnerPlayer.Session?.Send(result.RewardPackets.WinnerPacket);
                    LeaveGame(winnerPlayer.Id);
                }
                break;
            }

            case GameMode.Rank:
            {
                loserPlayer?.Session?.Send(result.RewardPackets.LoserPacket);
                winnerPlayer?.Session?.Send(result.RewardPackets.WinnerPacket);

                if (loserPlayer != null) LeaveGame(loserPlayer.Id);
                if (winnerPlayer != null) LeaveGame(winnerPlayer.Id);
                break;
            }

            case GameMode.Single:
            {
                if (context.WinnerIsHuman)
                {
                    if (winnerPlayer != null && result.SinglePacketWinner != null)
                    {
                        winnerPlayer.Session?.Send(result.SinglePacketWinner);
                        LeaveGame(winnerPlayer.Id);
                    }
                    // loser는 승/패 팝업을 보내고 싶은지 정책. 기존 코드는 loser에게 패킷 안 보냈음.
                    if (loserPlayer != null) LeaveGame(loserPlayer.Id);
                }
                else
                {
                    if (loserPlayer != null && result.SinglePacketLoser != null)
                    {
                        loserPlayer.Session?.Send(result.SinglePacketLoser);
                        LeaveGame(loserPlayer.Id);
                    }
                    if (winnerPlayer != null) LeaveGame(winnerPlayer.Id);
                }
                break;
            }

            case GameMode.Tutorial:
            {
                if (winnerPlayer != null && result.TutorialPacket != null)
                {
                    winnerPlayer.Session?.Send(result.TutorialPacket);
                    LeaveGame(winnerPlayer.Id);
                }
                if (loserPlayer != null) LeaveGame(loserPlayer.Id);
                break;
            }

            case GameMode.Friendly:
            {
                if (loserPlayer != null && result.FriendlyPacketLoser != null)
                    loserPlayer.Session?.Send(result.FriendlyPacketLoser);

                if (winnerPlayer != null && result.FriendlyPacketWinner != null)
                    winnerPlayer.Session?.Send(result.FriendlyPacketWinner);

                if (loserPlayer != null) LeaveGame(loserPlayer.Id);
                if (winnerPlayer != null) LeaveGame(winnerPlayer.Id);
                break;
            }
        }

        // 룸 종료
        RoomActivated = false;

        // [ACTOR] 전역 변경은 GameLogic job으로 serialize 추천
        GameLogic.Instance.Push(() => GameLogic.Instance.RemoveGameRoom(context.RoomId));
    }
    
    private bool TryDecideWinner(out int winnerUserId, out int loserUserId)
    {
        winnerUserId = -1;
        loserUserId = -1;
        
        var sheepPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Sheep);
        var wolfPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Wolf);
        if (sheepPlayer == null || wolfPlayer == null) return false;
        
        int sheepUserId = sheepPlayer.Session?.UserId ?? -1;
        int wolfUserId = wolfPlayer.Session?.UserId ?? -1;
        var assetId = (SheepId)sheepPlayer.AssetId;
        var primeSheep = _sheeps.Values.FirstOrDefault(sheep => sheep.SheepId == assetId);
        if (primeSheep != null && _portal != null) return false;
        
        if (primeSheep == null)
        {
            Console.WriteLine($"[Room {RoomId}]{DateTime.Now} - Game Over, Mode: {GameMode}, Wolf win.");
            winnerUserId = wolfUserId;
            loserUserId  = sheepUserId;
            return true;
        }
        
        if (_portal?.Room == null)
        {
            Console.WriteLine($"[Room {RoomId}]{DateTime.Now} - Game Over, Mode: {GameMode}, Sheep win.");
            winnerUserId = sheepUserId;
            loserUserId  = wolfUserId;
            return true;
        }

        return false;
    }
    
    private void TrackUnits()
    {
        _towerTracker.Observe(_towers.Values, GameInfo.NorthMaxTower);
        _statueTracker.Observe(_statues.Values, GameInfo.NorthMaxMonster);

        if (!_notifiedTowerExcess && _towerTracker.HasAnyExcessThisRound)
        {
            SendUpkeepWarning(Faction.Sheep);
        }
        
        if (!_notifiedStatueExcess && _statueTracker.HasAnyExcessThisRound)
        {
            SendUpkeepWarning(Faction.Wolf);
        }
    }

    private void SendUpkeepWarning(Faction faction)
    {
        var player = _players.Values.FirstOrDefault(p => p.Faction == faction);
        if (player == null) return;

        var messageKey = faction == Faction.Sheep
            ? "warning_in_game_tower_upkeep"
            : "warning_in_game_statue_upkeep";
        player.Session?.Send(new S_SendWarningInGame { MessageKey = messageKey });
                
        if (faction == Faction.Wolf)
        {
            _notifiedStatueExcess = true;   
        }
        else
        {
            _notifiedTowerExcess = true;
        }
    }
    
    private void CheckMonsters()
    {
        if (GameMode == GameMode.Tutorial)
        {
            var npc = FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Sheep });
            if (npc != null && GameInfo.FenceStartPos.Z >= 2) return;
        }
        
        if (GameInfo.FenceStartPos.Z >= 10) return;
        
        if (!IsThereAnyMonster())
        {
            MoveForwardTowerAndFence();
        }
    }

    public bool IsThereAnyMonster()
    {
        return _monsters.Values.Any(monster => monster.Targetable || monster.Hp > 0);
    }

    private void MoveForwardTowerAndFence()
    {
        if (_checked) return;
    
        GameInfo.FenceCenter = GameInfo.FenceCenter with { Z = GameInfo.FenceCenter.Z + _forwardParam * 0.5f };
        GameInfo.FenceSize = GameInfo.FenceSize with { Z = GameInfo.FenceSize.Z + _forwardParam };
        GameInfo.FenceStartPos = GameInfo.FenceStartPos with { Z = GameInfo.FenceStartPos.Z + _forwardParam };

        Broadcast(new S_PlaySound { Sound = Sounds.ExpandFence, SoundType = SoundType.D2 });
        Broadcast(new S_SetLinePos { LinePos = GameInfo.FenceStartPos.Z });

        var towerCopyKeys = new HashSet<int>(_towers.Keys);
        for (int i = 0; i < GameData.NorthFenceMax; i++)
        {
            int fenceOrder = i;
            int minX = -12 + i * 2;
            int maxX = -10 + i * 2;
            int delayMs = 30 * i;
            
            // [ACTOR] 룸 타이머에 지연 job 등록 (다른 스레드/await 없음)
            PushAfter(delayMs, () => MoveForwardStep(minX, maxX, fenceOrder));
        }
        
        int finalizeDelay = 30 * GameData.NorthFenceMax + 30;
        float fencePosZSnapshot = GameInfo.FenceStartPos.Z;
        
        PushAfter(finalizeDelay, () => FinalizeMoveForward(towerCopyKeys, fencePosZSnapshot));
    }

    private void MoveForwardStep(int minX, int maxX, int fenceOrder)
    {
        if (_gameOverState != GameOverState.Running) return;

        try
        {
            var newFenceCellPos = GameInfo.FenceStartPos + new Vector3 { X = fenceOrder * 2 };
            var fence = _fences.Values.FirstOrDefault(f => f.CellPos.X >= minX && f.CellPos.X < maxX);

            if (fence != null)
            {
                Map.ApplyMap(fence, newFenceCellPos);
                fence.BroadcastInstantMove();
            }
            else
            {
                if (Map.CanSpawnFence(Map.Vector3To2(newFenceCellPos), _towers.Values.ToArray()))
                {
                    fence = SpawnFence(newFenceCellPos);
                }
                else
                {
                    _players.Values
                        .FirstOrDefault(p => p.Faction == Faction.Sheep)?.Session?
                        .Send(new S_SendWarningInGame { MessageKey = "warning_in_game_obstacles_between_fences" });
                }
            }

            if (fence != null)
            {
                SpawnEffect(EffectId.MoveForwardEffect, fence, fence);
            }
            
            // Move forward towers
            var towers = _towers.Values.Where(t => t.CellPos.X >= minX && t.CellPos.X < maxX).ToList();
            foreach (var tower in towers)
            {
                Map.ApplyMap(tower, tower.CellPos + new Vector3 { Z = _forwardParam });
                tower.BroadcastInstantMove();
                SpawnEffect(EffectId.MoveForwardEffect, tower, tower);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during tower and fence move forward: {e.Message}");
        }
    }

    private void FinalizeMoveForward(HashSet<int> towerCopyKeys, float fencePosZAfter)
    {
        try
        {
            var remainTowers = _towers
                .Where(kv => !towerCopyKeys.Contains(kv.Key)).Select(kv => kv.Value).ToList();

            foreach (var tower in remainTowers)
            {
                Map.ApplyMap(tower, tower.CellPos + new Vector3 { Z = _forwardParam });
                tower.BroadcastInstantMove();
                SpawnEffect(EffectId.MoveForwardEffect, tower, tower);
            }

            var z = fencePosZAfter;
            var statues = _statues.Values
                .Where(s => z >= s.CellPos.Z + (s.SizeZ - 1)).ToList();
            
            if (statues.Count > 0)
            {
                var primeSheep = _sheeps.Values.MinBy(s => s.SheepId);
                if (primeSheep != null)
                {
                    foreach (var statue in statues)
                        statue.OnDamaged(primeSheep, 9999, Damage.True);
                }
            }

            GameInfo.FenceMovedThisRound = true;
            TutorialExpandFenceHandler(z);
            
            _checked = true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in FinalizeMoveForward: {e.Message}");
        }
    }
    
    private void TutorialExpandFenceHandler(float fencePosZ)
    {
        if (GameMode != GameMode.Tutorial) return;

        var player = _players.Values.FirstOrDefault(p => !p.IsNpc);
        if (player == null) return;

        if (player.Faction == Faction.Sheep)
        {
            if (_checked) return;
            if (fencePosZ >= 10)
            {
                // [ACTOR] 2초 지연 후 룸 컨텍스트에서 spawn 수행
                PushAfter(2000, () =>
                {
                    // 중간에 상태가 바뀌었을 수 있으니 방어 (정책)
                    if (_checked) return;

                    SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(-3, 0, 2));
                    SpawnTowerOnRelativeZ(UnitId.FlowerPot, new Vector3(3, 0, 2));
                });
            }
        }
        else
        {
            _tutorialTrigger.TryTrigger(player, Faction.Wolf,
                "BattleWolf.AlertExpand",
                false,
                () => player.IsNpc == false
            );
        }
    }
    
    private void InitRound()
    {
        RoundTime = 24;
        _round++;
        TutorialSpawnFlag = false;
        _checked = false;

        SpawnTowersInNewRound();
        SpawnMonstersInNewRound();
        ManageResource();
        InitAiProperties();
    }

    private void ManageResource()
    {
        ApplyUpkeep();
        _notifiedTowerExcess = false;
        _notifiedStatueExcess = false;
        GameInfo.WolfResource += GameInfo.TotalWolfYield;
        GameInfo.WolfResource -= GameInfo.WolfUpkeep;
        GameInfo.SheepResource -= GameInfo.SheepUpkeep;
        GameInfo.SheepUpkeep = 0;
        GameInfo.WolfUpkeep = 0;
    }

    private void InitAiProperties()
    {
        GameInfo.FenceDamageThisRound = 0;
        GameInfo.SheepDamageThisRound = 0;
        GameInfo.SheepDeathsThisRound = 0;
        GameInfo.FenceMovedThisRound = false;
    }
    
    private void ApplyUpkeep()
    {
        var towerExcessList = _towerTracker.FinalizeAndReset();
        var statueExcessList = _statueTracker.FinalizeAndReset();

        foreach (var towerExcess in towerExcessList)
        {
            if (DataManager.UnitDict.TryGetValue((int)towerExcess.UnitId, out var unitData))
            {
                GameInfo.SheepUpkeep += CalcUpkeepCost(unitData.Id, towerExcess.PeakExcess);
            }
        }

        foreach (var statueExcess in statueExcessList)
        {
            if (DataManager.UnitDict.TryGetValue((int)statueExcess.UnitId, out var unitData))
            {
                GameInfo.WolfUpkeep += CalcUpkeepCost(unitData.Id, statueExcess.PeakExcess);
            }
        }
    }
    
    private int CalcUpkeepCost(int unitId, int excessCount)
    {
        if (DataManager.UnitDict.TryGetValue(unitId, out var unitData))
        {
            double upkeepCost = unitData.Stat.RequiredResources / (double)_upkeepParam;
            return (int)(upkeepCost * Math.Pow(3, excessCount - 1));
        }

        return 0;
    }
    
    private int CheckStars(Faction faction)
    {
        if (faction == Faction.Sheep)
        {
            if (GameInfo.TheNumberOfDestroyedSheep == 0)
            {
                return GameInfo.TheNumberOfDestroyedFence == 0 ? 3 : 2;
            }

            return 1;
        }

        if (faction == Faction.Wolf)
        {
            if (Round <= 15)
            {
                return GameInfo.FenceStartPos.Z < 2 ? 3 : 2;
            }

            return 1;
        }

        return 0;
    }
}