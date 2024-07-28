using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private readonly Scheduler _scheduler = new();
    
    private void CheckMonsters()
    {
        if (GameInfo.FenceStartPos.Z >= 10) return;
        
        if (_monsters.Values.Any(monster => monster.Targetable || monster.Hp > 0) == false)
        {
            MoveForwardTowerAndFence();
        }
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
            
            tasks.Add(_scheduler.ScheduleEvent(88 * i, () =>
            {
                try
                {
                    // Move forward fence
                    var newFenceCellPos = GameInfo.FenceStartPos + new Vector3 { X = fenceOrder * 2 };
                    var fence = _fences.Values
                        .FirstOrDefault(fence => fence.CellPos.X >= minX && fence.CellPos.X < maxX);
                    var newFence = SpawnFence(newFenceCellPos, StorageLevel);
                    if (fence != null)  LeaveGame(fence.Id);
                    SpawnEffect(EffectId.MoveForwardEffect, newFence);
                    
                    // Move forward towers
                    var towers = _towers.Values
                        .Where(tower => tower.CellPos.X >= minX && tower.CellPos.X < maxX).ToList();
                    foreach (var tower in towers)
                    {
                        tower.CellPos += new Vector3 { Z = _forwardParam };
                        tower.BroadcastMoveForward();
                        SpawnEffect(EffectId.MoveForwardEffect, tower);
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
                SpawnEffect(EffectId.MoveForwardEffect, tower);
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
        _tutorialSet = false;
        _checked = false;
        
        SpawnTowersInNewRound();
    }
}