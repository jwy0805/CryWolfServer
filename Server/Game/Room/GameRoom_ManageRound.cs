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

        var tasks = new List<Task>();
    
        for (int i = 0; i < GameData.NorthFenceMax; i++)
        {
            var minX = 2 * i - 12;
            var maxX = 2 * i - 10;
            var fences = _fences.Values.Where(fence => 
                fence.CellPos.X >= minX && fence.CellPos.X < maxX).ToList();
            var towers = _towers.Values.Where(tower => 
                tower.CellPos.X >= minX && tower.CellPos.X < maxX).ToList();

            tasks.Add(_scheduler.ScheduleEvent(88 * i, () =>
            {
                try
                {
                    foreach (var fence in fences)
                    {   
                        var newFenceCellPos = fence.CellPos + new Vector3 { Z = _forwardParam };
                        var newFenceLv = fence.FenceNum;
                        LeaveGame(fence.Id);
                        var newFence = SpawnFence(newFenceCellPos, newFenceLv);
                        SpawnEffect(EffectId.MoveForwardEffect, newFence);
                    }
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