using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.AI;
using SkillType = Server.Data.SkillType;

namespace Server.Game;

public partial class GameRoom
{
    private void InitAiServices()
    {
        if (Npc == null) return;
        var policy = new AiPolicy();
        var heuristics = new HeuristicsService(this);
        var factory = new ActionFactory(heuristics, policy);
        _aiController = new AiController(factory, policy);

        var aiUnits = Npc.UnitIds
            .SelectMany(id => new[] { (int)id, (int)(id - 1), (int)(id - 2) }).ToList();
        GameInfo.AiSkills = DataManager.SkillDict
            .Where(kv => aiUnits.Contains(kv.Key / 10))
            .Select(kv => kv.Value)
            .ToHashSet();
        GameInfo.MainSkills = GameInfo.AiSkills
            .Where(data => data.type == SkillType.Main)
            .ToDictionary(data => (UnitId)(data.id / 10), data => (Skill)(data.id));
    }
    
    private AiBlackboard BuildBlackboard()
    {
        if (_storage == null || _portal == null || Npc == null) return new AiBlackboard();
        
        var blackboard = new AiBlackboard
        {
            Faction = Npc.Faction,
            Resource = Npc.Faction == Faction.Sheep ? GameInfo.SheepResource : GameInfo.WolfResource,
            NpcBaseLevel = Npc.Faction == Faction.Sheep ? _storage.Level : _portal.Level,
            PlayerBaseLevel = Npc.Faction == Faction.Sheep ? _portal.Level : _storage.Level,
            RoundTimeLeft = RoundTime,
            PopulationPerKind = (Npc.Faction == Faction.Sheep ? GameInfo.NorthMaxTower : GameInfo.NorthMaxMonster) / 3
        };
        
        if (blackboard.Faction == Faction.Sheep)
        {
            blackboard.AntiAircraft = _statues.Values.Any(ms => DataManager.UnitDict[(int)ms.UnitId].stat.AttackType == 2);
        }

        blackboard.TotalPressure = CalcPressure(blackboard);
        
        return blackboard;
    }

    private float CalcPressure(AiBlackboard blackboard)
    {
        int myValue;
        int enemyValue;
        float pressureByFight;
        if (Npc?.Faction == Faction.Sheep)
        {
            myValue = _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId)).Sum();
            enemyValue = _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId)).Sum();
            pressureByFight = GetSheepPressure(blackboard);
        }
        else
        {
            myValue = _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId)).Sum();
            enemyValue = _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId)).Sum();
            pressureByFight = GetWolfPressure(blackboard);
        }

        float pressureByValue = (enemyValue - myValue) / (float)enemyValue * 10;

        return pressureByValue + pressureByFight;
    }

    private int GetUnitValue(int unitId)
    {
        if (Npc == null) return 0;
        var unit = DataManager.UnitDict[unitId];
        var mod = unitId % 100 % 3;
        var level = mod switch
        {
            1 => 1,
            2 => 2,
            0 => 3,
            _ => 0 // fallback
        };

        var unitValue =  GameManager.Instance.UnitValueMatrix[(level, unit.unitClass)];
        var skillVale = Npc.SkillUpgradedList
            .Where(s => s.ToString().Contains(((UnitId)unitId).ToString()))
            .Select(s =>
            {
                if (DataManager.SkillDict[(int)s].type == SkillType.Main)
                {
                    return unitValue / 4;
                }
                else
                {
                    return unitValue / 10;
                }
            }).Sum();
        
        return unitValue + skillVale;
    }

    private float GetSheepPressure(AiBlackboard blackboard)
    {
        var rangedMonsters = _monsters.Values.Count(statue => 
            DataManager.UnitDict[(int)statue.UnitId].unitRole == Role.Ranger ||
            DataManager.UnitDict[(int)statue.UnitId].unitRole == Role.Mage);
        var rangedTowers = _towers.Values.Count(tower => 
            DataManager.UnitDict[(int)tower.UnitId].unitRole == Role.Ranger ||
            DataManager.UnitDict[(int)tower.UnitId].unitRole == Role.Mage);
        var meleeMonsters = _monsters.Values.Count(statue => 
            DataManager.UnitDict[(int)statue.UnitId].unitRole == Role.Warrior ||
            DataManager.UnitDict[(int)statue.UnitId].unitRole == Role.Tanker);
        var meleeTowers = _towers.Values.Count(tower => 
            DataManager.UnitDict[(int)tower.UnitId].unitRole == Role.Warrior ||
            DataManager.UnitDict[(int)tower.UnitId].unitRole == Role.Tanker);
        var rangedDiff = rangedMonsters - rangedTowers;
        var meleeDiff = meleeMonsters - meleeTowers;

        if (rangedDiff <= 0 && meleeDiff <= 0) return 0;
        return rangedDiff * blackboard.RangedFactor + meleeDiff * blackboard.MeleeFactor;
    }

    private float GetWolfPressure(AiBlackboard blackboard)
    {
        var monsterCount = _monsters.Count;
        var fenceZ = GameInfo.FenceStartPos.Z;
        float pressureByFence = (fenceZ - (-10)) / 4f * blackboard.FenceFactor;
        float pressureByTime = 0f;
        if (monsterCount == 0)
        {
            pressureByTime = RoundTime * blackboard.TimeFactor;
        }
        
        return pressureByFence + pressureByTime;
    }

    public float CalcEconomicScore(AiBlackboard blackboard, double min = -5, double max = 5)
    {
        if (Npc == null) return 0f;
        float score = 0f;
        if (Npc.Faction == Faction.Sheep)
        {
            if (_monsters.Count > 0) return score;
            score += blackboard.RoundTimeLeft * blackboard.RoundTimeLeftFactor;
            return score;   
        }
        else // value가 앞서고 있다면 -5, 5 사이의 난수를 정규분포 확률에 따라 score로 취득 
        {
            var myValue = _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId)).Sum();
            var enemyValue = _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId)).Sum();

            if (myValue > enemyValue)
            {
                score += (float)Util.Util.GetRandomValueByGaussian(_random, min, max, 0, 1.5f);
            }
        }

        return score;
    }

    public float FenceHealthScore()
    {
        if (Npc == null || Npc.Faction == Faction.Wolf) return -10000f;
        if (_fences.Values.Any(fence => fence.Hp * 2 < fence.MaxHp)) return 3f;
        if (_fences.Values.Any(fence => fence.Hp * 4 < fence.MaxHp)) return 6f;
        return 0.5f;
    }

    public float StatueHealthScore()
    {
        if (Npc == null || Npc.Faction == Faction.Sheep) return -10000f;
        if (_statues.Values.Any(statue => statue.Hp * 2 < statue.MaxHp)) return 3f;
        if (_statues.Values.Any(statue => statue.Hp * 4 < statue.MaxHp)) return 6f;
        return 0.5f;
    }
    
    public UnitId PickCounterUnit(Faction faction, AiBlackboard blackboard)
    {
        if (Npc == null) return UnitId.UnknownUnit;
        var unitRoles = new[] { Role.Warrior, Role.Ranger, Role.Mage, Role.Supporter, Role.Tanker };
        var unitRoleDict = unitRoles.ToDictionary(r => r, _ => 0f);
        var enemies = faction == Faction.Sheep 
            ? _statues.Values.Cast<GameObject>().ToList() : _towers.Values.Cast<GameObject>().ToList();
        
        foreach (var gameObject in enemies)
        {
            var unitId = gameObject switch
            {
                Tower tower => tower.UnitId,
                MonsterStatue statue => statue.UnitId,
                _ => default
            };

            if (unitId == default) continue;
            
            var unit = DataManager.UnitDict[(int)unitId];
            foreach (var role in unitRoles)
            {
                if (GameManager.Instance.RoleAffinityMatrix.TryGetValue((unit.unitRole, role), out var score))
                {
                    unitRoleDict[role] += score;
                }
            }
        }
            
        var frontRoles = new[] { Role.Warrior, Role.Tanker };
        var backRoles = new[] { Role.Ranger, Role.Mage };
            
        var bestFrontRole = frontRoles.OrderByDescending(r => unitRoleDict[r]).First();
        var bestBackRole = backRoles.OrderByDescending(r => unitRoleDict[r]).First();
        
        // 자원, 기존 타워 위치들 고려
        var unitPool = Npc.UnitIds;
        if (blackboard.UnitProb < 1)
        {
            var frontUnits = unitPool
                .Select(id => DataManager.UnitDict[(int)id])
                .Where(u => u.unitRole == bestFrontRole)
                .ToList();
            
            if (frontUnits.Count > 0)
            {
                return (UnitId)frontUnits[new Random().Next(frontUnits.Count)].id;
            }
        }
        else
        {
            var backUnits = unitPool
                .Select(id => DataManager.UnitDict[(int)id])
                .Where(u => u.unitRole == bestBackRole)
                .ToList();

            if (backUnits.Count > 0)
            {
                return (UnitId)backUnits[new Random().Next(backUnits.Count)].id;
            }
        }
        
        return UnitId.UnknownUnit;
    }

    public Vector3 SampleTowerPos(UnitId unitId)
    {
        var data = DataManager.UnitDict[(int)unitId];
        bool isFrontliner = data.unitRole is Role.Warrior or Role.Tanker;
        if (_statues.Count == 0) return SampleBaseCandidate(isFrontliner);

        double baseSigmaCenter = isFrontliner ? 2.5 : 4.0;
        double centerExp = isFrontliner ? 1.0 : 0.8;        // 센터 바이어스 지수
        double weightThreshold = 0.03;
        const double distMinimum = 1;                    // 아군 타워 최소거리
        const double sigmaRepulsion = 0.25;                 // 간격 위반시 패널티 폭
        const double beta = 0.75;                           // 적 타워 x를 0으로 압축하는 비율
        const double sigmaStatue = 1.5;                     // 적 석상 x 끌림 폭
        
        double sigmaCenter = ComputeAdaptiveCenterSigma(baseSigmaCenter);
        
        var towersXZ = Snapshot2D();
        var statueXs = SnapshotStatueX();
        Console.WriteLine($"[DEBUG] statueXs.Count={statueXs.Count} [{string.Join(",", statueXs.Take(5))}]");
        
        int nCandidates = 10;
        int nXperZ = 16;
        float xMin = -10;
        float xMax = 10;
        
        double bestLogWeight = double.NegativeInfinity;
        Vector3 best = default;
        
        var candidates = new List<Vector3>(nCandidates);
        var minDists = new List<double>(nCandidates);
        for (int i = 0; i < nCandidates; i++)
        {
            var vector = SampleBaseCandidate(isFrontliner);
            float z = vector.Z;
            var (x, logWeight, minDistFromTower) = 
                SampleXWithWeight(z, sigmaCenter, centerExp, xMin, xMax, nXperZ, towersXZ, statueXs,
                distMinimum, sigmaRepulsion, beta, sigmaStatue);
            
            var candidate = new Vector3(x, 6, z);
            candidates.Add(candidate);
            minDists.Add(minDistFromTower);

            if (logWeight > bestLogWeight)
            {
                bestLogWeight = logWeight;
                best = candidate;
            }
        }
        
        // 1) 충분히 좋은 후보면 반환
        if (Math.Exp(bestLogWeight) >= weightThreshold) return best;
        
        // 2) 간단 휴리스틱 폴백(추가 샘플 없이)
        int bestIdx = -1;
        double bestScore = double.NegativeInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            double d = minDists[i];
            
            // 중앙 선호: |x|가 작을수록 점수↑ (0~1 스케일로 간단히)
            double centerScore = 1.0 / (1.0 + Math.Abs(c.X));
            // 간격 선호: dPref에 가까울수록 점수↑
            double gapScore   = 1.0 / (1.0 + Math.Abs(d - distMinimum));
            // 너무 가까우면 큰 페널티
            double collisionPenalty = (d < distMinimum * 0.8) ? -100.0 : 0.0;
            
            double score = 0.5 * gapScore + 0.5 * centerScore + collisionPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }
        
        return bestIdx >= 0 ? candidates[bestIdx] : best;
    }
    
    public Vector3 SampleStatuePos(UnitId unitId)
    {
        var data = DataManager.UnitDict[(int)unitId];
        float min;
        float mean;
        float max;
        if (data.unitRole is Role.Warrior or Role.Tanker)
        {
            min = GameInfo.FenceStartPos.Z + 4;
            mean = min < 8 ? 12 : 14; // fence z cord -8, -4, 0, 4, 8, 12 
            max = GameData.PortalPos.Z - 6;
        }
        else // Ranger, Mage, Supporter
        {
            min = GameInfo.FenceStartPos.Z + 5;
            mean = min < 9 ? 13 : 15; // fence z cord -8, -4, 0, 4, 8, 12
            max = GameData.PortalPos.Z - 5;
        }
        
        var candidates = new List<Vector3>();
        for (int i = 0; i < 20; i++)
        {
            var x = Util.Util.GetRandomValueByGaussian(_random, -10, 10, 0, 4);
            var z = Util.Util.GetRandomValueByPert(_random, min, max, mean);
            candidates.Add(new Vector3((float)x, 6, (float)z));
        }

        var weights = new List<double>();
        foreach (var c in candidates)
        {
            double w = 1.0f;
            foreach (var statue in _statues.Values)
            {
                double dist = Vector3.Distance(c, statue.CellPos);
                w *= GetGaussianWeight(dist);
            }

            weights.Add(w);
        }

        double total = weights.Sum();
        double value = _random.NextDouble() * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            value -= weights[i];
            if (value <= 0)
            {
                return candidates[i];
            }
        }
            
        return candidates.Last(); 
    }
    
    public Skill PickSkillToUpgrade(Faction faction, AiBlackboard blackboard)
    {
        if (Npc == null) return Skill.NoSkill;
        var resource = faction == Faction.Sheep ? GameInfo.SheepResource : GameInfo.WolfResource;
        var maxCost = (int)(resource * blackboard.SkillCostLimit);
        var candidates = new List<(UnitId unit, Skill skill, int distToMain, bool upgradableNow)>();

        foreach (var kv in GameInfo.MainSkills)
        {
            var unit = kv.Key;
            var main = kv.Value;
            var pick = PickNextTowardsMain(main, out var distToMain, out var upgradableNow);
            if (pick == Skill.NoSkill) continue;
            
            var cost = DataManager.SkillDict[(int)pick].cost;
            if (cost > maxCost) continue;
            
            // (PickNextTowardsMain이 2단계 이내만 반환)
            candidates.Add((unit, pick, distToMain, upgradableNow));
        }

        if (candidates.Count == 0) return Skill.NoSkill;
        
        // 우선순위: 1) 지금 당장 찍을 수 있는가 2) 메인까지 거리가 더 짧은가 3) 선택 안정화(원하는 tie-break 넣기)
        var best = candidates
            .OrderByDescending(c => c.upgradableNow)   // true 우선
            .ThenBy(c => c.distToMain)                 // 메인까지 남은 단계 적을수록 우선
            .ThenBy(c => (int)c.unit)                  // (선택) 유닛 ID 순 tie-break
            .First();

        return best.skill;
    }

    public UnitId PickUnitToUpgrade(AiBlackboard blackboard)
    {
        if (Npc == null) return UnitId.UnknownUnit;
        var units = Npc.UnitIds.Where(id => id != UnitId.UnknownUnit).ToArray();
        var dict = DataManager.UnitDict;
        if (units.Length == 0) return UnitId.UnknownUnit;
        
        // 진화 가능 유닛(3레벨 미만)
        var upgradables = units.Where(unitId => (int)unitId % 100 % 3 != 0).ToList();
        
        // Supporter 진화 확률(상황 무관 7~15%) - 확률은 매 결정 시 살짝 흔들리도록 Uniform[MIN, MAX]
        double supporterProb = blackboard.SupporterProbMin +
                               (blackboard.SupporterProbMax - blackboard.SupporterProbMin) * _random.NextDouble();
        if (_random.NextDouble() < supporterProb)
        {
            var supporters = units.Where(id => dict[(int)id].unitRole == Role.Supporter).ToList();
            if (supporters.Count > 0)
            {
                return supporters[_random.Next(supporters.Count)];
            }
        }
        
        // 각 유닛에 대한 “결정 점수 + 소량 지터” 계산
        var scoreByUnit = new Dictionary<UnitId, double>(upgradables.Count());
        foreach (var unitId in upgradables)
        {
            double score = ComputeUnitUpgradeScore(unitId, blackboard);
            score += blackboard.JitterScale * _random.NextDouble();
            scoreByUnit[unitId] = Math.Max(1e-9, score);
        }
        
        // 확률적 선택
        return SampleBySoftmax(scoreByUnit, blackboard.TempSoftmax, blackboard.EpsilonExplore);
    }
    
    public void Ai_SpawnTower(UnitId towerId, PositionInfo pos, int cost)
    {
        var player = Npc;
        var tower = SpawnTower(towerId, pos, player);
        SpawnEffect(EffectId.Upgrade, tower, tower);
        if (player == null) return;
        if (player.Faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
        }
        else if (player.Faction == Faction.Wolf)
        {
            GameInfo.WolfResource -= cost;
        }
    }
    
    public void Ai_SpawnStatue(UnitId monsterId, PositionInfo pos, int cost)
    {
        var player = Npc;
        var statue = SpawnMonsterStatue(monsterId, pos, player);
        SpawnEffect(EffectId.Upgrade, statue, statue);
        if (player == null) return;
        if (player.Faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
        }
        else if (player.Faction == Faction.Wolf)
        {
            GameInfo.WolfResource -= cost;
        }
    }
    
    public void Ai_UpgradeSkill(Skill skill, int cost)
    {
        var player = Npc;
        if (player == null) return;
        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
        
        if (player.Faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
        }
        else if (player.Faction == Faction.Wolf)
        {
            GameInfo.WolfResource -= cost;
        }
    }
    
    public void Ai_UpgradeUnit(UnitId prevUnitId, int cost)
    {
        DataManager.UnitDict.TryGetValue((int)prevUnitId, out var unitData);
        if (unitData == null || Npc == null) return;
        if(Enum.TryParse(unitData.faction, out Faction faction) == false) return;
        
        UpdateRemainSkills(Npc, prevUnitId);
        if (Npc.Faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
            var towers = _towers.Values.Where(tower => tower.UnitId == prevUnitId).ToList();
            foreach (var tower in towers)
            {
                UpgradeUnit(tower, Npc);
            }
        }
        else if (Npc.Faction == Faction.Wolf)
        {
            GameInfo.WolfResource -= cost;
            var statues = _statues.Values.Where(statue => statue.UnitId == prevUnitId).ToList();
            foreach (var statue in statues)
            {
                UpgradeUnit(statue, Npc);
            }
        }
    }
    
    public void Ai_RepairAllFences(int cost)
    {
        GameInfo.SheepResource -=  cost; 
        foreach (var fence in _fences.Values)
        {
            fence.Hp = fence.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = fence.Id, Hp = fence.Hp });
        }
    }
    
    public void Ai_RepairAllStatues(int cost)
    {
        GameInfo.WolfResource -= cost;
        foreach (var statue in _statues.Values)
        {
            statue.Hp = statue.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = statue.Id, Hp = statue.Hp });
        }
    }
    
    public void Ai_UpgradeStorage(int cost)
    {
        if (_storage is not { Level: < 3 }) return;
        GameInfo.SheepResource -= cost;
        _storage.LevelUp();
    }
    
    public void Ai_UpgradePortal(int cost)
    {
        if (_portal is not { Level: < 3 }) return;
        GameInfo.WolfResource -= cost;
        _portal.LevelUp();
    }
}