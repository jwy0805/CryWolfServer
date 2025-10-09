using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.AI;

public static class AiActions
{
    private const float SpawnPopulationFactor = 1.1f;
    private const float SpawnValueFactor = 0.9f;
    public const float UpgradePopulationFactor = 0.9f;
    public const float UpgradeValueFactor = 1.1f;
    private const float SkillUpgradeFactor1 = 0.8f;
    private const float SkillUpgradeFactor2 = 1.2f;

    public static IAiAction SpawnUnit(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new SpawnUnitAction(unitId, pos, heuristics, policy, blackboard);
    public static IAiAction UpgradeSkill(Skill skill, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new SkillUpgradeAction(skill, heuristics, policy, blackboard);
    public static IAiAction UpgradeUnit(UnitId unitId, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new UnitUpgradeAction(heuristics, policy, unitId, blackboard);
    public static IAiAction RepairFence(IHeuristicsService heuristics) => new RepairFenceAction(heuristics);
    public static IAiAction RepairStatue(IHeuristicsService heuristics) => new RepairStatueAction(heuristics);
    public static IAiAction RepairPortal(IHeuristicsService heuristics) => new RepairPortalAction(heuristics);
    public static IAiAction UpgradeStorage(IHeuristicsService heuristics) => new UpgradeStorageAction(heuristics);
    public static IAiAction UpgradePortal(IHeuristicsService heuristics) => new UpgradePortalAction(heuristics);
    public static IAiAction UpgradeEnchant(IHeuristicsService heuristics) => new UpgradeEnchantAction(heuristics);
    public static IAiAction SpawnSheep(IHeuristicsService heuristics, AiBlackboard blackboard) => new SpawnSheepAction(heuristics, blackboard);
    public static IAiAction UpgradeYield(IHeuristicsService heuristics, Faction faction) 
        => new UpgradeYieldAction(heuristics, faction);

    private sealed class SpawnUnitAction : IAiAction
    {
        private readonly UnitId _unitId;
        private readonly PositionInfo _pos;  // Considering position of the fences
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly int _cost;
        private readonly AiBlackboard _blackboard;
        
        public SpawnUnitAction(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _unitId = unitId;
            _pos = pos;
            _heuristics = heuristics;
            _policy = policy;
            _cost = DataManager.UnitDict.TryGetValue((int)unitId, out var unitData) ? unitData.stat.RequiredResources : 0;
            _blackboard = blackboard;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard) * SpawnValueFactor;
            var populationFactor1 = _heuristics.ComparePopulation(blackboard, _policy) * SpawnPopulationFactor;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SpawnPopulationFactor;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return totalPressure + populationFactor1 + populationFactor2 + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            if (_blackboard.MyFaction == Faction.Sheep)
            {
                room.Push(room.Ai_SpawnTower, _unitId, _pos, _cost, _blackboard.MyPlayer);
            }            
            else
            {
                room.Push(room.Ai_SpawnStatue, _unitId, _pos, _cost, _blackboard.MyPlayer);
            }
        }
    }
    
    private sealed class SkillUpgradeAction : IAiAction
    {
        private readonly Skill _skill;
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly int _cost;
        private readonly AiBlackboard _blackboard;
        
        public SkillUpgradeAction(Skill skill, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _skill = skill;
            _heuristics = heuristics;
            _policy = policy;
            _cost = DataManager.SkillDict.TryGetValue((int)skill, out var skillData) ? skillData.cost : 0;
            _blackboard = blackboard;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard);
            var populationFactor1 = _heuristics.ComparePopulation(blackboard, _policy) * SkillUpgradeFactor1;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SkillUpgradeFactor2;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return totalPressure + populationFactor1 + populationFactor2 + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeSkill, _skill, _cost, _blackboard.MyPlayer);
        }
    }
    
    private sealed class UnitUpgradeAction : IAiAction
    {
        private readonly UnitId _unitId; // 업그레이드할 유닛
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly int _cost;
        private readonly AiBlackboard _blackboard;
        
        public UnitUpgradeAction(IHeuristicsService heuristics, AiPolicy policy, UnitId unitId, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _policy = policy;
            _unitId = unitId;
            _cost = heuristics.Room.CalcUpgradeCost((int)unitId);
            _blackboard = blackboard;
        }

        public float Score(AiBlackboard blackboard)
        {
            var totalPressure = _heuristics.EvaluatePressure(blackboard);
            var populationFactor1 = _heuristics.ComparePopulation(blackboard, _policy) * SkillUpgradeFactor1;
            var populationFactor2 = _heuristics.EvaluatePopulation(blackboard) * SkillUpgradeFactor2;
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            var upgradable = _heuristics.Room.VerifyTechForUnitUpgrade(blackboard.MyFaction, _unitId);
            return upgradable ? totalPressure + populationFactor1 + populationFactor2 + resourceFactor : -1000;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeUnit, _unitId, _cost, _blackboard.MyPlayer);
        }
    }
    
    private sealed class RepairFenceAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        
        public RepairFenceAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcFenceRepairCost();
        }
        
        public float Score(AiBlackboard blackboard)
        {
            return _heuristics.Room.FenceHealthScore(blackboard.MyFaction);
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_RepairAllFences, _cost);
        }
    }
    
    private sealed class RepairStatueAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;

        public RepairStatueAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = _heuristics.Room.CalcStatueRepairCost();
        }

        public float Score(AiBlackboard blackboard)
        {
            return _heuristics.Room.StatueHealthScore(blackboard.MyFaction);
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_RepairAllStatues, _cost);
        }
    }
    
    private sealed class RepairPortalAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        
        public RepairPortalAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcPortalRepairCost();
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = _heuristics.Room.PortalHealthScore();
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return score + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_RepairPortal, _cost);
        }
    }
    
    private sealed class UpgradeStorageAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        
        public UpgradeStorageAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcBaseUpgradeCost();
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = _heuristics.Room.UpgradeStorageScore(blackboard);
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return score + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeStorage, _cost);
        }
    }

    private sealed class UpgradePortalAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        
        public UpgradePortalAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcPortalUpgradeCost();
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = _heuristics.Room.UpgradePortalScore(blackboard);
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return score + resourceFactor;
        }
        
        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradePortal, _cost);
        }
    }
    
    private sealed class UpgradeEnchantAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        
        public UpgradeEnchantAction(IHeuristicsService heuristics)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcEnchantUpgradeCost();
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = _heuristics.Room.UpgradeEnchantScore();
            var resourceFactor = _heuristics.EvaluateResource(blackboard, _cost);
            return score + resourceFactor;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeEnchant, _cost);
        }
    }

    #region Economic Actions
    
    private sealed class SpawnSheepAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        private readonly AiBlackboard _blackboard;
        
        public SpawnSheepAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcSheepCost();
            _blackboard = blackboard;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = 0f;
            score += _heuristics.NeedEconomicUpgrade(blackboard);
            score += _heuristics.EvaluateResource(blackboard, _cost);
            return score;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_SpawnSheep, _cost, _blackboard.MyPlayer);
        }
    }
    
    private sealed class UpgradeYieldAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly int _cost;
        private readonly Faction _myFaction;
        
        public UpgradeYieldAction(IHeuristicsService heuristics, Faction faction)
        {
            _heuristics = heuristics;
            _cost = heuristics.Room.CalcEconomyUpgradeCost(faction);
            _myFaction = faction;
        }
        
        public float Score(AiBlackboard blackboard)
        {
            var score = 0f;
            score += _heuristics.NeedEconomicUpgrade(blackboard);
            score += _heuristics.EvaluateResource(blackboard, _cost);
            return score;
        }

        public void Execute(GameRoom room)
        {
            room.Push(room.Ai_UpgradeEconomy, _cost, _myFaction);
        }
    }
    
    #endregion
}