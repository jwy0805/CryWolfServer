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
        // 1) 유닛 소환
        var unitId = PickUnit(blackboard, room);
        blackboard.MyCounts.TryGetValue(unitId, out var count);
        if (_policy.UpkeepTolerance <= 0 && count + 1 > blackboard.PopulationPerKind) yield break;

        // spawn unit
        var vector = SamplePosition(blackboard, unitId, room);
        var pos = new PositionInfo { PosX = vector.X, PosY = vector.Y, PosZ = vector.Z };
        yield return AiActions.SpawnUnit(unitId, pos, _heuristics, _policy, blackboard);
        
        // 2) 스킬 업그레이드
        var skill = PickSkill(blackboard, room);
        if (skill != Skill.NoSkill)
        {
            yield return AiActions.UpgradeSkill(skill, _heuristics, _policy, blackboard);
        }
        
        // 3) 유닛 업그레이드(진화)
        var upgradeUnitId = UpgradeUnit(blackboard, room);
        if (upgradeUnitId != UnitId.UnknownUnit)
        {
            yield return AiActions.UpgradeUnit(upgradeUnitId, _heuristics, _policy, blackboard);
        }
        
        yield return AiActions.RepairFence(_heuristics);
        yield return AiActions.RepairStatue(_heuristics);
        yield return AiActions.RepairPortal(_heuristics);
        yield return AiActions.UpgradeStorage(_heuristics);
        yield return AiActions.UpgradePortal(_heuristics);
        yield return AiActions.UpgradeEnchant(_heuristics);
        yield return AiActions.SpawnSheep(_heuristics, blackboard);
        yield return AiActions.UpgradeYield(_heuristics, blackboard.MyFaction);
    }
    
    private UnitId PickUnit(AiBlackboard blackboard, GameRoom room)
    {
        return room.PickCounterUnit(blackboard.MyFaction, blackboard);
    }

    private Vector3 SamplePosition(AiBlackboard blackboard, UnitId unitId, GameRoom room)
    {
        /*
         sheep
         각 lane 별 statue 중간 지점
         최소거리 1
         약간 가운데 편향되도록
         직업군별 위치선정 다르게
        
         wolf
         최소거리 1
         맨 처음 위치는 랜덤
         전략에 따라 가끔 전진 statue 사용하되 평소엔 후방에 배치
         직업군별 위치선정 다르게        
        */
        return blackboard.MyFaction == Faction.Sheep ? room.SampleTowerPos(unitId) : room.SampleStatuePos(unitId);
    }

    private Skill PickSkill(AiBlackboard blackboard, GameRoom room)
    {
        return room.PickSkillToUpgrade(blackboard);
    }
    
    private UnitId UpgradeUnit(AiBlackboard blackboard, GameRoom room)
    {
        var unitToUpgrade = room.PickUnitToUpgrade(blackboard);
        return unitToUpgrade;
    }
}