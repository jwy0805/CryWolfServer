using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private bool _checked;
    private readonly int _forwardParam = 4;
    private Scheduler _scheduler = new();
    
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
        
        GameData.FenceCenter = GameData.FenceCenter with { Z = GameData.FenceCenter.Z + _forwardParam };
        GameData.FenceStartPos = GameData.FenceStartPos with { Z = GameData.FenceStartPos.Z + _forwardParam };
        
        for (int i = 0; i < GameData.NorthFenceMax; i++)
        {
            var minX = 2 * i - 12;
            var maxX = 2 * i - 10;
            var fences = _fences.Values.Where(fence => 
                fence.CellPos.X >= minX && fence.CellPos.X <= maxX).ToList();
            var towers = _towers.Values.Where(tower => 
                tower.CellPos.X >= minX && tower.CellPos.X <= maxX).ToList();

            await _scheduler.ScheduleEvent(111 * i, () =>
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
            });
        }
    }
    
    private void InitRound()
    {
        _checked = false;
    }
}