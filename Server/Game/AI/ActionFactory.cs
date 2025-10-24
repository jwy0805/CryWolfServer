using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public sealed class ActionFactory : IActionFactory
{
    private readonly IHeuristicsService _heuristics;
    private readonly AiPolicy _policy;
    
    public ActionFactory(IHeuristicsService heuristics, AiPolicy policy)
    {
        _heuristics = heuristics;
        _policy = policy;
    }
    
    public IEnumerable<IAiAction> Enumerate(AiBlackboard blackboard, GameRoom room)
    {
        if (room.RoomActivated == false) yield break;

        // 1) 유닛 소환
        var unitId = room.PickCounterUnit(blackboard.MyFaction, blackboard, _policy);
        blackboard.MyCounts.TryGetValue(unitId, out var count);
        if (_policy.UpkeepTolerance > 0 || count + 1 <= blackboard.PopulationPerKind)
        {
            // spawn unit
            if (unitId != UnitId.UnknownUnit)
            {
                var vector = SamplePosition(blackboard, unitId, room);
                var pos = new PositionInfo { PosX = vector.X, PosY = vector.Y, PosZ = vector.Z };
                yield return AiActions.SpawnUnit(unitId, pos, _heuristics, _policy, blackboard);
            }   
        }
        
        // 2) 스킬 업그레이드
        var skill = room.PickSkillToUpgrade(blackboard);
        if (skill != Skill.NoSkill)
        {
            yield return AiActions.UpgradeSkill(skill, _heuristics, _policy, blackboard);
        }
        
        // 3) 유닛 업그레이드(진화)
        var upgradeUnitId = room.PickUnitToUpgrade(blackboard);
        if (upgradeUnitId != UnitId.UnknownUnit)
        {
            yield return AiActions.UpgradeUnit(upgradeUnitId, _heuristics, _policy, blackboard);
        }
        
        yield return AiActions.UpgradeYield(_heuristics, _policy, blackboard);

        if (blackboard.MyFaction == Faction.Sheep)
        {
            yield return AiActions.RepairFence(_heuristics, blackboard);
            yield return AiActions.UpgradeStorage(_heuristics, blackboard);
            yield return AiActions.SpawnSheep(_heuristics, _policy, blackboard);
        }
        else
        {
            yield return AiActions.RepairStatue(_heuristics, blackboard);
            yield return AiActions.RepairPortal(_heuristics, blackboard);
            yield return AiActions.UpgradePortal(_heuristics, blackboard);
            yield return AiActions.UpgradeEnchant(_heuristics, blackboard);
        }
    }
    
    private Vector3 SamplePosition(AiBlackboard blackboard, UnitId unitId, GameRoom room)
    {
        return blackboard.MyFaction == Faction.Sheep ? room.SampleTowerPos(unitId) : room.SampleStatuePos(unitId);
    }
    
    public IAiAction CreateIdleAction(AiBlackboard blackboard) => AiActions.Idle(_heuristics, blackboard);
}