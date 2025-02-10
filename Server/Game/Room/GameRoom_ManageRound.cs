using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private readonly Scheduler _scheduler = new();

    private struct PlayerRewardPackets
    {
        public S_ShowRankResultPopup WinnerPacket;
        public S_ShowRankResultPopup LoserPacket;
    }

    private void CheckWinner()
    {
        var sheepPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Sheep);
        var wolfPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Wolf);
        if (sheepPlayer == null || wolfPlayer == null) return;
        
        var assetId = (SheepId)sheepPlayer.AssetId;
        var primeSheep = _sheeps.Values.FirstOrDefault(sheep => sheep.SheepId == assetId);
        
        if (primeSheep != null && _portal != null) return;
        if (primeSheep == null)
        {
            GameOver(wolfPlayer.Session?.UserId ?? -1, sheepPlayer.Session?.UserId ?? -1);
        }
        if (_portal?.Room == null)
        {
            GameOver(sheepPlayer.Session?.UserId ?? -1, wolfPlayer.Session?.UserId ?? -1);
        }
    }
    
    private void CheckMonsters()
    {
        if (GameInfo.FenceStartPos.Z >= 10) return;
        
        if (IsThereAnyMonster() == false)
        {
            MoveForwardTowerAndFence();
        }
    }

    public bool IsThereAnyMonster()
    {
        return _monsters.Values.Any(monster => monster.Targetable || monster.Hp > 0);
    }

    private async void MoveForwardTowerAndFence()
    {
        if (_checked) return;
        _checked = true;
    
        GameInfo.FenceCenter = GameInfo.FenceCenter with { Z = GameInfo.FenceCenter.Z + _forwardParam };
        GameInfo.FenceStartPos = GameInfo.FenceStartPos with { Z = GameInfo.FenceStartPos.Z + _forwardParam };

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
                        fence = SpawnFence(newFenceCellPos);
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
                // tower.CellPos += new Vector3 { Z = _forwardParam };
                tower.BroadcastInstantMove();
                SpawnEffect(EffectId.MoveForwardEffect, tower, tower);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MoveForwardTowerAndFence: {ex.Message}");
        }
    }
    
    private void InitRound()
    {
        _roundTime = 24;
        _round++;
        _singlePlayFlag = false;
        _checked = false;
        
        SpawnTowersInNewRound();
        SpawnMonstersInNewRound();
    }

    public async void GameOver(int winnerId, int loserId)
    {
        var loserPlayer = _players.Values
            .Where(player => player.Session?.UserId == loserId)
            .OrderByDescending(player => player.Session?.SessionId)
            .FirstOrDefault() ?? new Player();    
        var winnerPlayer = _players.Values
            .Where(player => player.Session?.UserId == winnerId)
            .OrderByDescending(player => player.Session?.SessionId)
            .FirstOrDefault() ?? new Player();

        if (GameMode == GameMode.Rank)
        {
            var packets = await GetRankReward(winnerPlayer, loserPlayer);

            Console.WriteLine($"{loserPlayer.Session?.UserId} vs {winnerPlayer.Session?.UserId}");
        
            loserPlayer.Session?.Send(packets.LoserPacket);
            winnerPlayer.Session?.Send(packets.WinnerPacket);
        
            LeaveGame(loserPlayer.Id);
            LeaveGame(winnerPlayer.Id);
        }
        else if (GameMode == GameMode.Single)
        {
            var packet = new S_ShowSingleResultPopup { Win = true  };

            if (winnerPlayer.Session?.UserId == winnerId)
            {
                var star = CheckStars(winnerPlayer.Faction);
                packet.Star = star;
                var rewardList = await GetSingleReward(winnerPlayer, star);
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
                
                winnerPlayer.Session?.Send(packet);
                LeaveGame(winnerPlayer.Id);
            }
            else
            {
                packet.Win = false;
                loserPlayer.Session?.Send(packet);
                LeaveGame(loserPlayer.Id);
            }
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
            if (GameInfo.TheNumberOfDestroyedFence == 0)
            {
                return GameInfo.FenceStartPos.Z < 2 ? 3 : 2;
            }

            return 1;
        }

        return 0;
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
}