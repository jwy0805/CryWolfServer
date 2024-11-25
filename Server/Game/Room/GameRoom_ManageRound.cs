using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private readonly Scheduler _scheduler = new();

    private void CheckWinner()
    {
        var sheepPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Sheep);
        var wolfPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Wolf);
        if (sheepPlayer == null || wolfPlayer == null) return;
        
        var assetId = (SheepId)sheepPlayer.AssetId;
        var primeSheep = _sheeps.Values.FirstOrDefault(sheep => sheep.SheepId == assetId);
        
        if (primeSheep != null && _portals.Values.Count > 0) return;
        if (primeSheep == null)
        {
            GameOver(wolfPlayer.Session?.UserId ?? -1, sheepPlayer.Session?.UserId ?? -1);
        }
        if (_portals.Values.Count == 0)
        {
            GameOver(sheepPlayer.Session?.UserId ?? -1, wolfPlayer.Session?.UserId ?? -1);
        }
    }
    
    private void CheckMonsters()
    {
        if (GameInfo.FenceStartPos.Z >= 7) return;
        
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
                        fence.CellPos = newFenceCellPos;
                        fence.BroadcastMoveForward();
                    }
                    else
                    {
                        fence = SpawnFence(newFenceCellPos, StorageLevel);
                    }
                    
                    SpawnEffect(EffectId.MoveForwardEffect, fence, fence);

                    // Move forward towers
                    var towers = _towers.Values
                        .Where(tower => tower.CellPos.X >= minX && tower.CellPos.X < maxX).ToList();
                    foreach (var tower in towers)
                    {
                        tower.CellPos += new Vector3 { Z = _forwardParam };
                        tower.BroadcastMoveForward();
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
            
            var diffTowers = _towers
                .Where(kv => !towerCopy.ContainsKey(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var tower in diffTowers.Values)
            {
                tower.CellPos += new Vector3 { Z = _forwardParam };
                tower.BroadcastMoveForward();
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
        _roundTime = 19;
        _round++;
        _tutorialSet = false;
        _checked = false;
        
        SpawnTowersInNewRound();
        SpawnMonstersInNewRound();
    }

    public async void GameOver(int winnerId, int loserId)
    {
        var loserPlayer = _players.Values.FirstOrDefault(player => player.Session?.UserId == loserId) ?? new Player();
        var winnerPlayer = _players.Values.FirstOrDefault(player => player.Session?.UserId == winnerId) ?? new Player();
        var rewardPacket = new GameRewardPacketRequired
        {
            WinUserId = winnerPlayer.Session?.UserId ?? -1,
            WinRankPoint = winnerPlayer.WinRankPoint,
            LoseUserId = loserPlayer.Session?.UserId ?? -1,
            LoseRankPoint = loserPlayer.LoseRankPoint
        };

        var task = NetworkManager.Instance
            .SendRequestToApiAsync<GameRewardPacketResponse>("Match/RankGameReward", rewardPacket, HttpMethod.Put);
        await task;

        if (task.Result == null)
        {
            Console.WriteLine("Game Over: Error in GameRewardPacketResponse");
            return;
        }
        
        var loserPacket = new S_ShowResultPopup
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
        
        var winnerPacket = new S_ShowResultPopup
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

        Console.WriteLine($"{loserPlayer.Session?.UserId} vs {winnerPlayer.Session?.UserId}");
        
        loserPlayer.Session?.Send(loserPacket);
        winnerPlayer.Session?.Send(winnerPacket);
        
        RoomActivated = false;
    }
}