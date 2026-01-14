using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private readonly int _upkeepParam = 4;
    private readonly Scheduler _scheduler = new();

    private struct PlayerRewardPackets
    {
        public S_ShowRankResultPopup WinnerPacket;
        public S_ShowRankResultPopup LoserPacket;
    }

    private async Task CheckRegular()
    {
        if (Round > 0)
        {
            await CheckWinner();
        }
        
        TrackUnits();
    }
    
    private async Task CheckWinner()
    {
        var sheepPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Sheep);
        var wolfPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Wolf);
        if (sheepPlayer == null || wolfPlayer == null) return;
        
        var assetId = (SheepId)sheepPlayer.AssetId;
        var primeSheep = _sheeps.Values.FirstOrDefault(sheep => sheep.SheepId == assetId);

        if (primeSheep != null && _portal != null) return;
        if (primeSheep == null)
        {
            Console.WriteLine($"[Room {RoomId}]{DateTime.Now} - Game Over, Mode: {GameMode}, Wolf win.");
            await GameOver(wolfPlayer.Session?.UserId ?? -1, sheepPlayer.Session?.UserId ?? -1);
            return;
        }
        
        if (_portal?.Room == null)
        {
            Console.WriteLine($"[Room {RoomId}]{DateTime.Now} - Game Over, Mode: {GameMode}, Sheep win.");
            await GameOver(sheepPlayer.Session?.UserId ?? -1, wolfPlayer.Session?.UserId ?? -1);
        }
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
        
        if (IsThereAnyMonster() == false)
        {
            _ = MoveForwardTowerAndFence();
        }
    }

    public bool IsThereAnyMonster()
    {
        return _monsters.Values.Any(monster => monster.Targetable || monster.Hp > 0);
    }

    private async Task MoveForwardTowerAndFence()
    {
        if (_checked) return;
    
        GameInfo.FenceCenter = GameInfo.FenceCenter with { Z = GameInfo.FenceCenter.Z + _forwardParam * 0.5f };
        GameInfo.FenceSize = GameInfo.FenceSize with { Z = GameInfo.FenceSize.Z + _forwardParam };
        GameInfo.FenceStartPos = GameInfo.FenceStartPos with { Z = GameInfo.FenceStartPos.Z + _forwardParam };

        Broadcast(new S_PlaySound { Sound = Sounds.ExpandFence, SoundType = SoundType.D2 });

        var towerCopy = new Dictionary<int, Tower>(_towers);
        var tasks = new List<Task>();
    
        for (int i = 0; i < GameData.NorthFenceMax; i++)
        {
            var minX = -12 + i * 2;
            var maxX = -10 + i * 2;
            var fenceOrder = i;
            
            tasks.Add(_scheduler.ScheduleEvent(30 * i, () =>
            {
                try
                {
                    // Move forward fence
                    var newFenceCellPos = GameInfo.FenceStartPos + new Vector3 { X = fenceOrder * 2 };
                    var fence = _fences.Values
                        .FirstOrDefault(fence => fence.CellPos.X >= minX && fence.CellPos.X < maxX);
                    
                    if (fence != null)
                    {
                        Map.ApplyMap(fence, newFenceCellPos);
                        fence.BroadcastInstantMove();
                    }
                    else
                    {
                        var oldCellPos = newFenceCellPos with { Z = newFenceCellPos.Z - _forwardParam };
                        var oldCellPosV2 = Map.Vector3To2(oldCellPos);
                        if (Map.CanSpawnFence(oldCellPosV2, _towers.Values.ToArray()))
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
                    
                    SpawnEffect(EffectId.MoveForwardEffect, fence, fence);

                    // Move forward towers
                    var towers = _towers.Values
                        .Where(tower => tower.CellPos.X >= minX && tower.CellPos.X < maxX).ToList();
                    foreach (var tower in towers)
                    {
                        Map.ApplyMap(tower, tower.CellPos + new Vector3 { Z = _forwardParam });
                        tower.BroadcastInstantMove();
                        SpawnEffect(EffectId.MoveForwardEffect, tower, tower);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during tower and fence move forward: {ex.Message}");
                }
            }));
        }

        try
        {
            await Task.WhenAll(tasks);
            
            var remainTowers = _towers
                .Where(kv => !towerCopy.ContainsKey(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var tower in remainTowers.Values)
            {
                Map.ApplyMap(tower, tower.CellPos + new Vector3 { Z = _forwardParam });
                tower.BroadcastInstantMove();
                SpawnEffect(EffectId.MoveForwardEffect, tower, tower);
            }
            
            var z = GameInfo.FenceStartPos.Z;
            var statues = _statues.Values
                .Where(s => z >= s.CellPos.Z + (s.SizeZ - 1)).ToList();
            if (statues.Any())
            {
                var primeSheep = _sheeps.Values.MinBy(s => s.SheepId);
                foreach (var statue in statues)
                {
                    if (primeSheep != null)
                    {
                        statue.OnDamaged(primeSheep, 9999, Damage.True);
                    }
                }
            }

            GameInfo.FenceMovedThisRound = true;
            
            // Tutorial
            await TutorialExpandFenceHandler(z);
            _checked = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MoveForwardTowerAndFence: {ex.Message}");
        }
    }

    private async Task TutorialExpandFenceHandler(float fencePosZ)
    {
        if (GameMode != GameMode.Tutorial) return;
        var player = _players.Values.FirstOrDefault(p => !p.IsNpc);
        if (player == null) return;
        if (player.Faction == Faction.Sheep)
        {
            if (_checked) return;
            if (fencePosZ >= 10)
            {
                await Task.Delay(2000);
                SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(-3, 0, 2));
                SpawnTowerOnRelativeZ(UnitId.FlowerPot, new Vector3(3, 0, 2));
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

    #region GameOver&Rewards

    public async Task GameOver(int winnerId, int loserId)
    {
        _gameOver = true;

        var loserPlayer = _players.Values
            .Where(player => player.Session?.UserId == loserId)
            .OrderByDescending(player => player.Session?.SessionId)
            .FirstOrDefault() ?? new Player();    
        var winnerPlayer = _players.Values
            .Where(player => player.Session?.UserId == winnerId)
            .OrderByDescending(player => player.Session?.SessionId)
            .FirstOrDefault() ?? new Player();
        
        switch (GameMode)
        {
            case GameMode.Test:
                EndTestHandler(winnerPlayer, loserPlayer);
                break;
            case GameMode.Rank:
                await GetRankRewardHandler(winnerPlayer, loserPlayer);
                break;
            case GameMode.Single:
                await GetSingleRewardHandler(winnerPlayer, loserPlayer, winnerId);
                break;
            case GameMode.Tutorial:
                await GetTutorialRewardHandler(winnerPlayer, winnerId);
                break;
            case GameMode.Friendly:
                await FriendlyEndHandler(winnerPlayer, loserPlayer);
                break;
        }
        
        RoomActivated = false;
        GameLogic.Instance.RemoveGameRoom(RoomId);
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
    
    private void EndTestHandler(Player? winner, Player? loser)
    {
        if (loser != null)
        {
            var packet = new S_ShowRankResultPopup
            {
                Win = false, RankPointValue = 0, RankPoint = loser.RankPoint
            };
            loser.Session?.Send(packet);
            LeaveGame(loser.Id);
        }

        if (winner != null)
        {
            var packet = new S_ShowRankResultPopup
            {
                Win = true, RankPointValue = 0, RankPoint = winner.RankPoint
            };
            winner.Session?.Send(packet);
            LeaveGame(winner.Id);
        }
    }
    
    private async Task GetRankRewardHandler(Player winner, Player loser)
    {
        var packets = await GetRankReward(winner, loser);

        Console.WriteLine($"[GetRankRewardHandler] {loser.Session?.UserId} vs {winner.Session?.UserId}");
        
        loser.Session?.Send(packets.LoserPacket);
        winner.Session?.Send(packets.WinnerPacket);
        
        LeaveGame(loser.Id);
        LeaveGame(winner.Id);
    }
    
    private async Task<PlayerRewardPackets> GetRankReward(Player winnerPlayer, Player loserPlayer)
    {
        var rewardPacket = new RankGameRewardPacketRequired
        {
            WinUserId = winnerPlayer.Session?.UserId ?? -1,
            WinRankPoint = winnerPlayer.WinRankPoint,
            LoseUserId = loserPlayer.Session?.UserId ?? -1,
            LoseRankPoint = loserPlayer.LoseRankPoint
        };

        var packets = new PlayerRewardPackets();
        var task = NetworkManager.Instance.SendRequestToApiAsync<RankGameRewardPacketResponse>(
            "Match/RankGameReward", rewardPacket, HttpMethod.Put);
        await task;
        
        if (task.Result == null)
        {
            Console.WriteLine("Game Over: Error in GameRewardPacketResponse");
            return packets;
        }
        
        var loserPacket = new S_ShowRankResultPopup
        {
            Win = false, RankPointValue = loserPlayer.LoseRankPoint, RankPoint = loserPlayer.RankPoint
        };

        foreach (var rewardInfo in task.Result.LoserRewards)
        {
            loserPacket.Rewards.Add(new Reward
            {
                ItemId = rewardInfo.ItemId, ProductType = rewardInfo.ProductType, Count = rewardInfo.Count
            });
        }
        
        var winnerPacket = new S_ShowRankResultPopup
        {
            Win = true, RankPointValue = winnerPlayer.WinRankPoint, RankPoint = winnerPlayer.RankPoint
        };
        
        foreach (var rewardInfo in task.Result.WinnerRewards)
        {
            winnerPacket.Rewards.Add(new Reward
            {
                ItemId = rewardInfo.ItemId, ProductType = rewardInfo.ProductType, Count = rewardInfo.Count
            });
        }
        
        packets.WinnerPacket = winnerPacket;
        packets.LoserPacket = loserPacket;
        
        return packets;
    }

    private async Task GetSingleRewardHandler(Player winner, Player loser, int winnerId)
    {
        var packet = new S_ShowSingleResultPopup { Win = true  };

        if (winner.Session?.UserId == winnerId)
        {
            var star = CheckStars(winner.Faction);
            var rewardList = await GetSingleReward(winner, star);
            foreach (var rewardInfo in rewardList)
            {
                packet.SingleRewards.Add(new SingleReward
                {
                    ItemId = rewardInfo.ItemId,
                    ProductType = rewardInfo.ProductType,
                    Count = rewardInfo.Count,
                    Star = rewardInfo.Star
                });
            }

            packet.Star = star;
            winner.Session?.Send(packet);
            LeaveGame(winner.Id);
            
            await UserEventManager.Instance.EventProgressHandler(new List<int> { winnerId },
                RoomId, "single_play_clear_2026", EventCounterKey.single_play_win);
        }
        else
        {
            packet.Win = false;
            loser.Session?.Send(packet);
            LeaveGame(loser.Id);
        }
    }
    
    private async Task<List<SingleRewardInfo>> GetSingleReward(Player player, int star)
    {
        var rewardPacket = new SingleGameRewardPacketRequired
        {
            UserId = player.Session?.UserId ?? -1,
            StageId = StageId,
            Star = star,
        };
        
        var task = NetworkManager.Instance.SendRequestToApiAsync<SingleGameRewardPacketResponse>(
            "Match/SingleGameReward", rewardPacket, HttpMethod.Put);
        await task;
        
        if (task.Result == null)
        {
            Console.WriteLine("Game Over: Error in SingleGameRewardPacketResponse");
            return new List<SingleRewardInfo>();
        }
        
        return task.Result.Rewards;
    }

    private async Task GetTutorialRewardHandler(Player winner, int winnerId)
    {
        var packet = new S_SendTutorialReward();
        var rewardUnitId = await GetTutorialReward(winner);
        
        packet.RewardUnitId = rewardUnitId;
        winner.Session?.Send(packet);
        LeaveGame(winner.Id);
    }
    
    private async Task<UnitId> GetTutorialReward(Player player)
    {
        var rewardPacket = new TutorialRewardPacketRequired
        {
            UserId = player.Session?.UserId ?? -1,
            Faction = player.Faction
        };
        
        var res = await NetworkManager.Instance.SendRequestToApiAsync<TutorialRewardPacketResponse>(
            "Match/TutorialReward", rewardPacket, HttpMethod.Put);
        
        if (res == null)
        {
            Console.WriteLine("Game Over: Error in TutorialRewardPacketResponse");
            return UnitId.UnknownUnit;
        }

        return (UnitId)res.Rewards.First().ItemId;
    }

    private async Task FriendlyEndHandler(Player winner, Player loser)
    {
        var loserPacket = new S_ShowFriendlyResultPopup { Win = false };
        var winnerPacket = new S_ShowFriendlyResultPopup { Win = true };
        var userIds = new List<int>
        {
            loser.Session?.UserId ?? -1,
            winner.Session?.UserId ?? -1
        };
        
        loser.Session?.Send(loserPacket);
        winner.Session?.Send(winnerPacket);
        
        LeaveGame(loser.Id);
        LeaveGame(winner.Id);

        await UserEventManager.Instance.EventProgressHandler(
            userIds, RoomId, "friendly_match_2026", EventCounterKey.friendly_match);
    }

    #endregion
}