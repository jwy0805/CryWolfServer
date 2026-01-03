using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.AI;

public static class AiActions
{
    public static Action<int, UnitId>? OnSpawnUnit;
    public static Action<int, Skill>? OnUpgradeSkill;
    public static Action<int, UnitId>? OnUpgradeUnit;
    public static Action<int>? OnRepairFence;
    public static Action<int>? OnRepairStatue;
    public static Action<int>? OnRepairPortal;
    public static Action<int>? OnUpgradeStorage;
    public static Action<int>? OnUpgradePortal;
    public static Action<int>? OnUpgradeEnchant;
    public static Action<int>? OnSpawnSheep;
    public static Action<int>? OnUpgradeYield;
    
    private const float SpawnPopulationFactor = 1f;
    private const float SpawnValueFactor = 0.9f;
    private const float UpgradePopulationFactor = 1f;
    private const float UpgradeValueFactor = 1.1f;
    private const float SkillUpgradeFactor1 = 1.3f;

    public static IAiAction Idle(IHeuristicsService heuristics, AiBlackboard blackboard) 
        => new IdleAction(heuristics, blackboard);
    public static IAiAction SpawnUnit(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new SpawnUnitAction(unitId, pos, heuristics, policy, blackboard);
    public static IAiAction UpgradeSkill(Skill skill, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new SkillUpgradeAction(skill, heuristics, policy, blackboard);
    public static IAiAction UpgradeUnit(UnitId unitId, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new UnitUpgradeAction(heuristics, policy, unitId, blackboard);
    public static IAiAction RepairFence(IHeuristicsService heuristics, AiBlackboard blackboard)
        => new RepairFenceAction(heuristics, blackboard);
    public static IAiAction RepairStatue(IHeuristicsService heuristics, AiBlackboard blackboard)
        => new RepairStatueAction(heuristics, blackboard);
    public static IAiAction RepairPortal(IHeuristicsService heuristics, AiBlackboard blackboard) 
        => new RepairPortalAction(heuristics, blackboard);
    public static IAiAction UpgradeStorage(IHeuristicsService heuristics, AiBlackboard blackboard)
        => new UpgradeStorageAction(heuristics, blackboard);
    public static IAiAction UpgradePortal(IHeuristicsService heuristics, AiBlackboard blackboard)
        => new UpgradePortalAction(heuristics, blackboard);
    public static IAiAction UpgradeEnchant(IHeuristicsService heuristics, AiBlackboard blackboard)
        => new UpgradeEnchantAction(heuristics, blackboard);
    public static IAiAction SpawnSheep(IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        => new SpawnSheepAction(heuristics, policy, blackboard);
    public static IAiAction UpgradeYield(IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard) 
        => new UpgradeYieldAction(heuristics, policy, blackboard);

    private sealed class IdleAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        
        public IdleAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
        }
        
        public double Score()
        {
            return 0;
        }

        public void Execute(GameRoom room)
        {
            
        }
    }
    
    private sealed class SpawnUnitAction : IAiAction
    {
        private readonly UnitId _unitId;
        private readonly PositionInfo _pos;  // Considering position of the fences
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public SpawnUnitAction(UnitId unitId, PositionInfo pos, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _unitId = unitId;
            _pos = pos;
            _heuristics = heuristics;
            _policy = policy;
            _blackboard = blackboard;
            _cost = DataManager.UnitDict.TryGetValue((int)unitId, out var unitData) ? unitData.Stat.RequiredResources : 0;
        }
        
        public double Score()
        {
            var totalPressure = _heuristics.EvaluatePressure(_blackboard) * SpawnValueFactor;
            var populationFactor1 = _heuristics.ComparePopulation(_blackboard, _policy) * SpawnPopulationFactor;
            var populationFactor2 = _heuristics.EvaluatePopulation(_blackboard, _policy);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            var capacityLimit = _heuristics.VerifyCapacity(_blackboard, _policy);
            var score = totalPressure + populationFactor1 + populationFactor2 + resourceFactor + capacityLimit;
            _score = score;
            Console.WriteLine($"[Room {_heuristics.Room.RoomId}]SpawnUnitActionScore: {_score} (Pressure: {totalPressure}, PopComp: {populationFactor1}, PopEval: {populationFactor2}, Resource: {resourceFactor}), Remaining: {_blackboard.MyResource - _cost}");
            return _score;
        }

        public void Execute(GameRoom room)
        {
            if (_blackboard.MyFaction == Faction.Sheep)
            {
                room.Ai_SpawnTower(_unitId, _pos, _cost, _blackboard.MyPlayer);
            }            
            else
            {
                room.Ai_SpawnStatue(_unitId, _pos, _cost, _blackboard.MyPlayer);
            }
            OnSpawnUnit?.Invoke(_blackboard.MyPlayer.Id, _unitId);
        }
    }
    
    private sealed class SkillUpgradeAction : IAiAction
    {
        private readonly Skill _skill;
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public SkillUpgradeAction(Skill skill, IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _skill = skill;
            _heuristics = heuristics;
            _policy = policy;
            _blackboard = blackboard;
            _cost = DataManager.SkillDict.TryGetValue((int)skill, out var skillData) ? skillData.Cost : 0;
        }
        
        public double Score()
        {
            var totalPressure = _heuristics.EvaluatePressure(_blackboard);
            var populationFactor = _heuristics.ComparePopulation(_blackboard, _policy) * SkillUpgradeFactor1;
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            var score = totalPressure + populationFactor + resourceFactor;
            // Console.WriteLine($"[Room {_heuristics.Room.RoomId}]SkillUpgradeActionScore: {score} (Pressure: {totalPressure}, PopComp: {populationFactor}, Resource: {resourceFactor},  Remaining: {_blackboard.MyResource - _cost})");
            _score = score;
            return _score;
        }
        
        public void Execute(GameRoom room)
        {
            room.Ai_UpgradeSkill(_skill, _cost, _blackboard.MyPlayer);
            OnUpgradeSkill?.Invoke(_blackboard.MyPlayer.Id, _skill);
        }
    }
    
    private sealed class UnitUpgradeAction : IAiAction
    {
        private readonly UnitId _unitId; // 업그레이드할 유닛
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly int _cost;
        private readonly AiBlackboard _blackboard;
        private double _score;
        
        public UnitUpgradeAction(IHeuristicsService heuristics, AiPolicy policy, UnitId unitId, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _policy = policy;
            _unitId = unitId;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcUpgradeCost((int)unitId);
        }

        public double Score()
        {
            var totalPressure = _heuristics.EvaluatePressure(_blackboard);
            var populationFactor = _heuristics.ComparePopulation(_blackboard, _policy) * UpgradePopulationFactor;
            var valueFactor = _heuristics.CompareValue(_blackboard, _policy) * UpgradeValueFactor;
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            var upgradable = _heuristics.Room.VerifyTechForUnitUpgrade(_blackboard.MyFaction, _unitId);
            _score = totalPressure + populationFactor + valueFactor + resourceFactor;
            // Console.WriteLine($"[Room {_heuristics.Room.RoomId}]UnitUpgradeActionScore: {_score} (Pressure: {totalPressure}, PopComp: {populationFactor}, valueFactor: {valueFactor}, Resource: {resourceFactor},  Remaining: {_blackboard.MyResource - _cost})");
            return upgradable ? _score : -1000;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_UpgradeUnit(_unitId, _cost, _blackboard.MyPlayer);
            OnUpgradeUnit?.Invoke(_blackboard.MyPlayer.Id, _unitId);
        }
    }
    
    private sealed class RepairFenceAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public RepairFenceAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcFenceRepairCost();
        }
        
        public double Score()
        {
            var score = _heuristics.Room.FenceHealthScore(_blackboard.MyFaction);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_RepairAllFences(_cost);
            OnRepairFence?.Invoke(_blackboard.MyPlayer.Id);
        }
    }
    
    private sealed class RepairStatueAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public RepairStatueAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = _heuristics.Room.CalcStatueRepairCost();
        }

        public double Score()
        {
            var score = _heuristics.Room.StatueHealthScore(_blackboard.MyFaction);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_RepairAllStatues(_cost);
            OnRepairStatue?.Invoke(_blackboard.MyPlayer.Id);
        }
    }
    
    private sealed class RepairPortalAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public RepairPortalAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcPortalRepairCost();
        }
        
        public double Score()
        {
            var score = _heuristics.Room.PortalHealthScore();
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_RepairPortal(_cost);
            OnRepairPortal?.Invoke(_blackboard.MyPlayer.Id);
        }
    }
    
    private sealed class UpgradeStorageAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public UpgradeStorageAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcBaseUpgradeCost();
        }
        
        public double Score()
        {
            var score = _heuristics.Room.UpgradeStorageScore(_blackboard);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            Console.WriteLine($"[Room {_heuristics.Room.RoomId}]StorageUpgradeActionScore: {_score} (Score: {score}, Resource: {resourceFactor}, Remaining: {_blackboard.MyResource - _cost})");
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_UpgradeStorage(_cost);
            OnUpgradeStorage?.Invoke(_blackboard.MyPlayer.Id);
        }
    }

    private sealed class UpgradePortalAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public UpgradePortalAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcPortalUpgradeCost();
        }
        
        public double Score()
        {
            var score = _heuristics.Room.UpgradePortalScore(_blackboard);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            Console.WriteLine($"[Room {_heuristics.Room.RoomId}]PortalUpgradeActionScore: {_score} (Score: {score}, Resource: {resourceFactor}, Remaining: {_blackboard.MyResource - _cost})");
            return _score;
        }
        
        public void Execute(GameRoom room)
        {
            room.Ai_UpgradePortal(_cost);
            OnUpgradePortal?.Invoke(_blackboard.MyPlayer.Id);
        }
    }
    
    private sealed class UpgradeEnchantAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public UpgradeEnchantAction(IHeuristicsService heuristics, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcEnchantUpgradeCost();
        }
        
        public double Score()
        {
            var score = _heuristics.Room.UpgradeEnchantScore();
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            _score = score + resourceFactor;
            Console.WriteLine($"[Room {_heuristics.Room.RoomId}]UpgradeEnchantScore: {_score}");
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_UpgradeEnchant(_cost);
            OnUpgradeEnchant?.Invoke(_blackboard.MyPlayer.Id);
        }
    }

    #region Economic Actions
    
    private sealed class SpawnSheepAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public SpawnSheepAction(IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _heuristics = heuristics;
            if (!DataManager.ObjectDict.TryGetValue(blackboard.MyPlayer.AssetId, out var data))
            {
                throw new Exception($"Invalid AssetId for Sheep Spawn: {blackboard.MyPlayer.AssetId}");
            }
            _cost = _heuristics.Room.CalcSheepCost(data.Stat.RequiredResources);
            _policy = policy;
            _blackboard = blackboard;
        }
        
        public double Score()
        {
            var score = _heuristics.NeedEconomicUpgrade(_blackboard, _policy);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            // Console.WriteLine("SheepSpawnActionScore: " + (score + resourceFactor));
            _score = score + resourceFactor;
            return _score;
        }

        public void Execute(GameRoom room)
        {
            room.Ai_SpawnSheep(_cost, _blackboard.MyPlayer);
            OnSpawnSheep?.Invoke(_blackboard.MyPlayer.Id);
        }
    }
    
    private sealed class UpgradeYieldAction : IAiAction
    {
        private readonly IHeuristicsService _heuristics;
        private readonly AiPolicy _policy;
        private readonly AiBlackboard _blackboard;
        private readonly int _cost;
        private double _score;
        
        public UpgradeYieldAction(IHeuristicsService heuristics, AiPolicy policy, AiBlackboard blackboard)
        {
            _heuristics = heuristics;         
            _policy = policy;
            _blackboard = blackboard;
            _cost = heuristics.Room.CalcEconomyUpgradeCost(blackboard.MyFaction);
        }
        
        public double Score()
        {
            var score = _heuristics.NeedEconomicUpgrade(_blackboard, _policy);
            var resourceFactor = _heuristics.EvaluateResource(_blackboard, _cost);
            // Console.WriteLine("UpgradeYieldActionScore: " + (score + resourceFactor));
            _score = score + resourceFactor;
            return _score;
        }

        public void Execute(GameRoom room)
        {
            OnUpgradeYield?.Invoke(_blackboard.MyPlayer.Id);
            room.Ai_UpgradeEconomy(_cost, _blackboard.MyFaction);
        }
    }
    
    #endregion
}