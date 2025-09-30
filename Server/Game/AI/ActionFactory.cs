using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

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
        var npc = room.Npc;
        if (npc == null) yield break;
        
        // 1) 유닛 소환
        var unitId = PickUnit(blackboard, room);
        blackboard.MyCounts.TryGetValue(unitId, out var count);
        if (_policy.UpkeepTolerance <= 0 && count + 1 > blackboard.PopulationPerKind) yield break;

        // spawn unit
        var vector = SamplePosition(unitId, room);
        var pos = new PositionInfo { PosX = vector.X, PosY = vector.Y, PosZ = vector.Z };
        yield return AiActions.SpawnUnit(unitId, pos, _heuristics, _policy);
        
        // 2) 스킬 업그레이드
        var skill = PickSkill(blackboard, room);
        if (skill != Skill.NoSkill)
        {
            yield return AiActions.UpgradeSkill(skill, _heuristics, _policy);
        }
        
        // 3) 유닛 업그레이드(진화)
        var upgradeUnitId = UpgradeUnit(blackboard, room);
        if (upgradeUnitId != UnitId.UnknownUnit)
        {
            yield return AiActions.UpgradeUnit(upgradeUnitId, _heuristics, _policy);
        }
        
        
    }
    
    private UnitId PickUnit(AiBlackboard blackboard, GameRoom room)
    {
        var npc = room.Npc;
        return npc == null ? UnitId.UnknownUnit : room.PickCounterUnit(npc.Faction, blackboard);
    }

    private Vector3 SamplePosition(UnitId unitId, GameRoom room)
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
        var npc = room.Npc;
        if (npc == null) return Vector3.Zero;
        return npc.Faction == Faction.Sheep ? room.SampleTowerPos(unitId) : room.SampleStatuePos(unitId);
    }

    private Skill PickSkill(AiBlackboard blackboard, GameRoom room)
    {
        var npc = room.Npc;
        return npc == null ? Skill.NoSkill : room.PickSkillToUpgrade(npc.Faction, blackboard);
    }
    
    private UnitId UpgradeUnit(AiBlackboard blackboard, GameRoom room)
    {
        var npc = room.Npc;
        if (npc == null) return UnitId.UnknownUnit;
        var unitToUpgrade = room.PickUnitToUpgrade(blackboard);
        return unitToUpgrade;
    }
    
    private IEnumerable<UnitId> GetUnitPool(AiBlackboard blackboard)
    {
        return new[] { UnitId.UnknownUnit };
    }
}