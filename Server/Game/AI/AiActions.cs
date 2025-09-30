using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public static class AiActions
{
    private const float SpawnPopulationFactor = 1.1f;
    private const float SpawnValueFactor = 0.9f;
    public const float UpgradePopulationFactor = 0.9f;
    public const float UpgradeValueFactor = 1.1f;
    private const float SkillUpgradeFactor1 = 0.8f;
    private const float SkillUpgradeFactor2 = 1.2f;

    public static IAiAction SpawnUnit(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy) 
        => new SpawnUnitAction(unitId, pos, heuristics, policy);
    public static IAiAction UpgradeSkill(Skill skill, IHeuristicsService heuristics, AiPolicy policy) 
        => new SkillUpgradeAction(skill, heuristics, policy);
    public static IAiAction UpgradeUnit(UnitId unitId, IHeuristicsService heuristics, AiPolicy policy) 
        => new UnitUpgradeAction(heuristics, policy, unitId);
    public static IAiAction RepairFence(IHeuristicsService heuristics) => new RepairFenceAction(heuristics);
    public static IAiAction RepairStatue(IHeuristicsService heuristics) => new RepairStatueAction(heuristics);
    public static IAiAction RepairPortal(IHeuristicsService heuristics) => new RepairPortalAction(heuristics);
    public static IAiAction UpgradeStorage(IHeuristicsService heuristics) => new UpgradeStorageAction(heuristics);
    public static IAiAction UpgradeEnchant(IHeuristicsService heuristics) => new UpgradeEnchantAction(heuristics);
    public static IAiAction SpawnSheep(IHeuristicsService heuristics) => new SpawnSheepAction(heuristics);
    public static IAiAction UpgradeYield(IHeuristicsService heuristics) => new UpgradeYieldAction(heuristics);

    private sealed class SpawnUnitAction : IAiAction
    {
        private readonly UnitId _unitId;
        private readonly PositionInfo _pos;  // Considering position of the fences
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        
        public int Cost { get; set; }
        
        public SpawnUnitAction(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy)
        {
            _unitId = unitId;
            _pos = pos;
            _heuristics = heuristics;
            _policy = policy;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard) * SpawnValueFactor;
            var populationFactor1 = _heuristics.ComparePopulation(blackboard) * SpawnPopulationFactor;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SpawnPopulationFactor;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, Cost);
            return totalPressure + populationFactor1 + populationFactor2 + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            var npc = room.Npc;
            if (npc == null) return;
            if (npc.Faction == Faction.Sheep)
            {
                room.Push(room.Ai_SpawnTower, _unitId, _pos, Cost);
            }            
            else
            {
                room.Push(room.Ai_SpawnStatue, _unitId, _pos, Cost);
            }
        }
    }
    
    private sealed class SkillUpgradeAction : IAiAction
    {
        private readonly Skill _skill;
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        
        public int Cost { get; }
        
        public SkillUpgradeAction(Skill skill, IHeuristicsService heuristics, AiPolicy policy)
        {
            _skill = skill;
            _heuristics = heuristics;
            _policy = policy;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard);
            var populationFactor1 = _heuristics.ComparePopulation(blackboard) * SkillUpgradeFactor1;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SkillUpgradeFactor2;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, Cost);
            return totalPressure + populationFactor1 + populationFactor2 + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeSkill, _skill, Cost);
        }
    }
    
    private sealed class UnitUpgradeAction : IAiAction
    {
        private UnitId _unitId; // 업그레이드할 유닛
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;

        public int Cost { get; }
        
        public UnitUpgradeAction(IHeuristicsService heuristics, AiPolicy policy, UnitId unitId)
        {
            _heuristics = heuristics;
            _policy = policy;
            _unitId = unitId;
        }

        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard);
            var populationFactor1 = _heuristics.ComparePopulation(blackboard) * SkillUpgradeFactor1;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SkillUpgradeFactor2;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, Cost);
            return totalPressure + populationFactor1 + populationFactor2 + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeUnit, _unitId, Cost);
        }
    }
    
    private sealed class RepairFenceAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }

        public RepairFenceAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            return _heuristics.Room.FenceHealthScore();
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_RepairAllFences, Cost);
        }
    }
    
    private sealed class RepairStatueAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }

        public RepairStatueAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }

        public float Score(AiBlackboard blackboard)
        {
            return _heuristics.Room.StatueHealthScore();
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_RepairAllStatues, Cost);
        }
    }
    
    private sealed class RepairPortalAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }
        
        public RepairPortalAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            throw new NotImplementedException();
        }

        public void Execute(GameRoom room)
        {
            throw new NotImplementedException();
        }
    }
    
    private sealed class UpgradeStorageAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }
        
        public UpgradeStorageAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            throw new NotImplementedException();
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeStorage, Cost);
        }
    }
    
    private sealed class UpgradeEnchantAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }
        
        public UpgradeEnchantAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            throw new NotImplementedException();
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradePortal, Cost);
        }
    }

    #region Economic Actions
    
    private sealed class SpawnSheepAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }
        
        public SpawnSheepAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = 0f;
            score += _heuristics.NeedEconomicUpgrade(blackboard);
            score += _heuristics.EvaluateResource(blackboard, Cost);
            return score;
        }

        public void Execute(GameRoom room)
        {
            throw new NotImplementedException();
        }
    }
    
    private sealed class UpgradeYieldAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;

        public int Cost { get; }
        
        public UpgradeYieldAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = 0f;
            score += _heuristics.NeedEconomicUpgrade(blackboard);
            score += _heuristics.EvaluateResource(blackboard, Cost);
            return score;
        }

        public void Execute(GameRoom room)
        {
            // room.Push();
        }
    }
    
    #endregion
}