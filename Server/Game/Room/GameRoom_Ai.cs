using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.AI;
using SkillType = Server.Data.SkillType;

namespace Server.Game;

public partial class GameRoom
{
    private void InitAi()
    {
        switch (GameMode)
        {
            case GameMode.Single:
                InitSingleModeAi();
                break;
            case GameMode.AiTest:
                InitAiTestModeAi();
                SetAssets();
                break;
        }
    }
    
    private void InitAiServices(Player npc)
    {
        var policy = new AiPolicy();
        var heuristics = new HeuristicsService(this);
        var factory = new ActionFactory(heuristics, policy);
        var controller = new AiController(factory, policy)
        {
            AiSkills = DataManager.SkillDict
                .Where(kv => npc.AvailableUnits.Contains((UnitId)(kv.Key / 10)))  // UnitId 매핑 필요 시 수정
                .Select(kv => kv.Value)
                .ToHashSet(),
            MainSkills = DataManager.SkillDict
                .Where(kv => npc.AvailableUnits.Contains((UnitId)(kv.Key / 10)) && kv.Value.Type == SkillType.Main)
                .ToDictionary(kv => (UnitId)(kv.Value.Id / 10), kv => (Skill)(kv.Value.Id))
        };
        
        _aiControllers[npc.Faction] = controller;
        Console.WriteLine($"Init Ai Service For Player {npc.Id}");
    }

    private void InitSingleModeAi()
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true });
        if (npc == null)
        {
            Console.WriteLine("[Warning] AI player not found for Single mode.");
            return;
        }
        
        InitAiServices(npc);
    }

    private void InitAiTestModeAi()
    {
        var sheepNpc = FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Sheep });
        var wolfNpc = FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Wolf });
        if (sheepNpc == null || wolfNpc == null)
        {
            Console.WriteLine("[Warning] AI players not found for AiTest mode.");
            return;
        }
        
        InitAiServices(sheepNpc);
        InitAiServices(wolfNpc);
    }

    private WorldSnapshot BuildWorldSnapshot()
    {
        var sheepNpc = FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Sheep });
        var wolfNpc = FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Wolf });
        
        return new WorldSnapshot
        {
            SheepPlayer = sheepNpc ?? new Player(),
            WolfPlayer = wolfNpc ?? new Player(),
            SheepUnits = sheepNpc?.CurrentUnitIds ?? Array.Empty<UnitId>(),
            WolfUnits = wolfNpc?.CurrentUnitIds ?? Array.Empty<UnitId>(),
            RoundTimeLeft = RoundTime,
            SheepResource = GameInfo.SheepResource,
            WolfResource = GameInfo.WolfResource,
            SheepBaseLevel = _storage?.Level ?? 1,
            WolfBaseLevel = _portal?.Level ?? 1,
            SheepMaxPop = GameInfo.NorthMaxTower,
            SheepPop = _towers.Values.Count,
            WolfMaxPop = GameInfo.NorthMaxMonster,
            WolfPop = _statues.Values.Count,
            SheepUnitCounts = _towers.Values
                .GroupBy(tower => tower.UnitId)
                .ToDictionary(g => g.Key, g => g.Count()),
            WolfUnitCounts = _statues.Values
                .GroupBy(statue => statue.UnitId)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
    
    private AiBlackboard BuildBlackboard(WorldSnapshot snapshot, Faction faction, AiPolicy policy)
    {
        Player player = faction == Faction.Sheep ? snapshot.SheepPlayer : snapshot.WolfPlayer;
        bool amISheep = faction == Faction.Sheep;
        UnitId[] myUnits = amISheep ? snapshot.SheepUnits : snapshot.WolfUnits;
        int myResource = amISheep ? snapshot.SheepResource : snapshot.WolfResource;
        int myBaseLevel = amISheep ? snapshot.SheepBaseLevel : snapshot.WolfBaseLevel;
        int myMaxPop = amISheep ? snapshot.SheepMaxPop : snapshot.WolfMaxPop;
        int myPop = amISheep ? snapshot.SheepPop : snapshot.WolfPop;
        int enemyResource = amISheep ? snapshot.WolfResource : snapshot.SheepResource;
        int enemyBaseLevel = amISheep ? snapshot.WolfBaseLevel : snapshot.SheepBaseLevel;
        int enemyMaxPop = amISheep ? snapshot.WolfMaxPop : snapshot.SheepMaxPop;
        int enemyPop = amISheep ? snapshot.WolfPop : snapshot.SheepPop;
        var myCounts = amISheep ? snapshot.SheepUnitCounts : snapshot.WolfUnitCounts;
        var enemyCounts = amISheep ? snapshot.WolfUnitCounts : snapshot.SheepUnitCounts;
        var skillReady = new Dictionary<Skill, bool>();
        float unitProb = ComputeUnitProb(faction);
        
        AiBlackboard blackboard = new AiBlackboard(player, faction, myUnits, snapshot.RoundTimeLeft, myResource, myBaseLevel, myMaxPop, myPop,
            myCounts, enemyResource, enemyBaseLevel, enemyMaxPop, enemyPop, enemyCounts, skillReady, unitProb, policy)
        {
            PopulationPerKind = Math.Max(1, myCounts.Count),
            TotalPressure = CalcPressure(faction, policy, enemyResource)
        };
        Console.WriteLine($"blackboard built, pressure : {blackboard.TotalPressure}");
        return blackboard;
    }

    private double CalcPressure(Faction faction, AiPolicy policy, int enemyResource)
    {
        int myValue;
        int enemyValue;
        double pressureByBattle;
        if (faction == Faction.Sheep)
        {
            myValue = GetTowerValue();
            enemyValue = GetStatueValue();
            var enemyPlayer = FindPlayer(go => go is Player { Faction: Faction.Wolf });
            if (enemyPlayer == null) return 0;
            pressureByBattle = CalcSheepPressure(policy, enemyPlayer, enemyResource);
            if (GameInfo.UrgentSpawn) pressureByBattle += policy.UrgentSpawnValue;
        }
        else
        {
            myValue = GetStatueValue();
            enemyValue = GetTowerValue();
            pressureByBattle = CalcWolfPressure(policy);
        }

        double pressureByValue = policy.CalcPressureByValue(myValue, enemyValue);
        Console.WriteLine($"pressure by value: {pressureByValue}");
        return pressureByValue + pressureByBattle;
    }

    public int GetTowerValue() => _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId, Faction.Sheep)).Sum();
    public int GetStatueValue() => _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId, Faction.Wolf)).Sum();
    
    private int GetUnitValue(int unitId, Faction faction)
    {
        var npc = faction == Faction.Sheep 
            ? FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Sheep }) 
            : FindPlayer(go => go is Player { IsNpc: true, Faction: Faction.Wolf });
        if (npc == null) return 0;
        var unit = DataManager.UnitDict[unitId];
        var mod = unitId % 100 % 3;
        var level = mod switch
        {
            1 => 1,
            2 => 2,
            0 => 3,
            _ => 0 // fallback
        };

        var unitValue =  GameManager.Instance.UnitValueMatrix[(level, unit.UnitClass)];
        var skillVale = npc.SkillUpgradedList
            .Where(s => s.ToString().Contains(((UnitId)unitId).ToString()))
            .Select(s =>
            {
                if (DataManager.SkillDict[(int)s].Type == SkillType.Main)
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

    private double CalcSheepPressure(AiPolicy policy, Player enemyPlayer, int enemyResource)
    {
        double currentPressure = 0;
        double potentialPressure = 0;
        
        // Current Pressure
        var rangedMonsters = _monsters.Values.Count(statue => 
            DataManager.UnitDict[(int)statue.UnitId].UnitRole == Role.Ranger ||
            DataManager.UnitDict[(int)statue.UnitId].UnitRole == Role.Mage);
        var rangedTowers = _towers.Values.Count(tower => 
            DataManager.UnitDict[(int)tower.UnitId].UnitRole == Role.Ranger ||
            DataManager.UnitDict[(int)tower.UnitId].UnitRole == Role.Mage);
        var meleeMonsters = _monsters.Values.Count(statue => 
            DataManager.UnitDict[(int)statue.UnitId].UnitRole == Role.Warrior ||
            DataManager.UnitDict[(int)statue.UnitId].UnitRole == Role.Tanker);
        var meleeTowers = _towers.Values.Count(tower => 
            DataManager.UnitDict[(int)tower.UnitId].UnitRole == Role.Warrior ||
            DataManager.UnitDict[(int)tower.UnitId].UnitRole == Role.Tanker);
        var rangedDiff = rangedMonsters - rangedTowers;
        var meleeDiff = meleeMonsters - meleeTowers;

        if (rangedDiff > 0 || meleeDiff > 0)
        {
            currentPressure = policy.CalcCurrentPressure(rangedDiff, meleeDiff);
        }
        
        // Potential Pressure
        var enemyUnitIds = enemyPlayer.CurrentUnitIds;
        potentialPressure = CalcPotentialPressure(enemyUnitIds, enemyResource);

        return currentPressure + potentialPressure;
    }

    private double CalcPotentialPressure(UnitId[] enemyUnitIds, int enemyResource)
    {
        if (enemyUnitIds.Length == 0 || enemyResource <= 0) return 0;

        var items = enemyUnitIds.Distinct().Select(id =>
            {
                var unitData = DataManager.UnitDict[(int)id];
                int cost = unitData.Stat.RequiredResources;
                int value = (int)unitData.UnitClass;
                return (Cost: cost, Value: value, Ratio: (double)value / cost);
            })
            .Where(v => v.Cost > 0 && v.Cost <= enemyResource)
            .OrderByDescending(v => v.Ratio)
            .ToList();

        if (items.Count == 0) return 0;
        
        // 무제한 배낭 문제 (Unbounded Knapsack Problem)
        int remaining = enemyResource;
        int totalValue = 0;
        var matrix = GameManager.Instance.UnitValueMatrix;

        foreach (var item in items)
        {
            if (remaining < item.Cost) continue;

            int count = remaining / item.Cost;
            if (count <= 0) continue;

            totalValue += count * item.Value;
            remaining -= count * item.Cost;
            if (remaining <= 0) break;
        }

        return Util.Util.ScaleValueByLog(totalValue, matrix.Values.Min(), matrix.Values.Max(), 0.25, 1);
    }

    private double CalcWolfPressure(AiPolicy policy)
    {
        var monsterCount = _monsters.Count;
        var fenceZ = GameInfo.FenceStartPos.Z;
        double pressureByFence = policy.CalcPressureByFence(fenceZ);
        double pressureByTime = 0f;
        if (monsterCount == 0)
        {
            pressureByTime = RoundTime * policy.TimeFactor;
        }
        
        return pressureByFence + pressureByTime;
    }
    
    public UnitId PickCounterUnit(Faction faction, AiBlackboard blackboard, AiPolicy policy)
    {
        var unitDict = DataManager.UnitDict;
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
            
            var unit = unitDict[(int)unitId];
            foreach (var role in unitRoles)
            {
                if (GameManager.Instance.RoleAffinityMatrix.TryGetValue((unit.UnitRole, role), out var score))
                {
                    unitRoleDict[role] += score;
                }
            }
        }

        // 자원, 기존 타워 위치들 고려
        var unitPool = blackboard.MyPlayer.CurrentUnitIds;
        var unitIdsFromSkill = new HashSet<UnitId>(blackboard.MyPlayer.SkillUpgradedList.Select(skill =>
            (UnitId)(DataManager.SkillDict[(int)skill].Id % 10)));
        
        // 모기, 두더지 대응
        if (faction == Faction.Sheep)
        {
            var sheepData = DataManager.ObjectDict[blackboard.MyPlayer.AssetId + 1];
            var damagedThreshold = sheepData.Stat.MaxHp * policy.SheepDamagedThreshold * GameInfo.SheepCount;
            if (GameInfo.SheepDamageThisRound <= damagedThreshold || GameInfo.SheepDeathsThisRound > 0)
            {
                if (_monsters.Values.Any(m => m.CellPos.Z <= GameInfo.FenceStartPos.Z))
                {
                    GameInfo.UrgentSpawn = true;
                    blackboard.UnitProb = 1;
                    unitPool = unitPool.Where(id => unitDict[(int)id].Stat.AttackType != 0).ToArray();
                }
            }
        }

        var filteredPool = unitPool.Where(id =>
        {
            var data = unitDict[(int)id];
            return faction == Faction.Sheep
                ? !_towerTracker.AlreadyMaxed((UnitId)data.Id, blackboard.MyMaxPop)
                : !_statueTracker.AlreadyMaxed((UnitId)data.Id, blackboard.MyMaxPop);
        }).ToArray();
        
        var availableRoles = new HashSet<Role>(filteredPool.Select(id => unitDict[(int)id].UnitRole));
        var frontRoles = new[] { Role.Warrior, Role.Tanker };
        var backRoles = new[] { Role.Ranger, Role.Mage };
        var frontRolePriority = frontRoles.OrderByDescending(_ => _random.Next()).ToList();
        var backRolePriority = backRoles.OrderByDescending(_ => _random.Next()).ToList();
        bool preferFront = blackboard.UnitProb <= policy.UnitRoleProb;
        
        IReadOnlyCollection<Role> preferredSet = preferFront ? frontRolePriority : backRolePriority;
        IReadOnlyCollection<Role> fallbackSet = preferFront ? backRolePriority : frontRolePriority;

        var roleFilter = new HashSet<Role>(preferredSet.Where(r => availableRoles.Contains(r)));
        if (roleFilter.Count == 0)
        {
            roleFilter = new HashSet<Role>(fallbackSet.Where(r => availableRoles.Contains(r)));
            if (roleFilter.Count == 0) roleFilter = availableRoles;
        }
        
        // 스킬이 많이 업그레이드 된 유닛 우선
        var candidates = filteredPool
            .Select(id => unitDict[(int)id])
            .Where(data => roleFilter.Contains(data.UnitRole))
            .OrderByDescending(data =>
                Util.Util.GetAllSubUnitIds((UnitId)data.Id).Count(subId => unitIdsFromSkill.Contains(subId)) +
                _random.NextDouble() * 2).ToList();
        
        if (candidates.Count > 0)
        {
            return (UnitId)candidates.First().Id;
        }
        
        return UnitId.UnknownUnit;
    }

    public Vector3 SampleTowerPos(UnitId unitId)
    {
        var data = DataManager.UnitDict[(int)unitId];
        bool isFrontliner = data.UnitRole is Role.Warrior or Role.Tanker;
        if (_statues.Count == 0) return SampleZBaseCandidate(isFrontliner);

        double baseSigmaCenter = isFrontliner ? 2.5 : 4.0;
        double centerExp = isFrontliner ? 1.0 : 0.8;        // 센터 바이어스 지수
        double weightThreshold = 0.03;
        const double distMinimum = 1;                       // 아군 타워 최소거리
        const double sigmaRepulsion = 0.25;                 // 간격 위반시 패널티 폭
        const double beta = 0.75;                           // 적 타워 x를 0으로 압축하는 비율
        const double sigmaStatue = 1.5;                     // 적 석상 x 끌림 폭
        
        double sigmaCenter = ComputeAdaptiveCenterSigma(baseSigmaCenter);
        var towersXZ = Snapshot2D();
        var statueXs = SnapshotStatueX();
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
            var vector = SampleZBaseCandidate(isFrontliner);
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
        if (data.UnitRole is Role.Warrior or Role.Tanker)
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
    
    public Skill PickSkillToUpgrade(AiBlackboard blackboard)
    {
        var resource = blackboard.MyFaction == Faction.Sheep ? GameInfo.SheepResource : GameInfo.WolfResource;
        var maxCost = (int)(resource * blackboard.Policy.SkillCostLimit);
        var candidates = new List<(UnitId unit, Skill skill, int distToMain, bool upgradableNow)>();
        var mainSkillDict = _aiControllers[blackboard.MyFaction].MainSkills
            .Where(pair => blackboard.MyPlayer.CurrentUnitIds.Contains(pair.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        foreach (var (unitId, skill) in mainSkillDict)
        {
            var pick = PickNextTowardsMain(skill, out var distToMain, out var upgradableNow, blackboard.MyPlayer);
            if (pick == Skill.NoSkill) continue;
            
            var cost = DataManager.SkillDict[(int)pick].Cost;
            if (cost > maxCost) continue;
            
            // (PickNextTowardsMain이 2단계 이내만 반환)
            candidates.Add((unit: unitId, pick, distToMain, upgradableNow));
        }

        if (candidates.Count == 0)
        {
            var fieldUnits = blackboard.MyFaction == Faction.Sheep
                ? _towers.Values.Select(t => t.UnitId).ToList()
                : _statues.Values.Select(ms => ms.UnitId).ToList();

            var fieldCandidates = new List<(Skill skill, bool upNow, int cost, int unitCount)>();
            
            foreach (var unit in fieldUnits)
            {
                var skills = DataManager.SkillDict.Values
                    .Where(s => s.Id / 10 == (int)unit && !blackboard.MyPlayer.SkillUpgradedList.Contains((Skill)s.Id))
                    .ToList();

                foreach (var skillData in skills)
                {
                    var skill = DataManager.SkillDict[skillData.Id];
                    var cost = skill.Cost;
                    if (cost > maxCost) continue;

                    bool lackOfSkill = VerifySkillTree(blackboard.MyPlayer, (Skill)skill.Id);
                    var unitCount = fieldUnits.Count(u => u == unit);
                    fieldCandidates.Add(((Skill)skill.Id, !lackOfSkill, cost, unitCount));
                }
            }

            if (fieldCandidates.Count > 0)
            {
                var ordered = fieldCandidates
                    .Where(c => c.upNow)
                    .OrderByDescending(c => c.unitCount)
                    .FirstOrDefault();

                if (ordered != default) return ordered.skill;
            }
        }
        
        // 우선순위: 1) 지금 당장 찍을 수 있는가 2) 메인까지 거리가 더 짧은가 3) 선택 안정화(원하는 tie-break 넣기)
        var best = candidates
            .OrderByDescending(c => c.upgradableNow)   // true 우선
            .ThenBy(c => c.distToMain)                 // 메인까지 남은 단계 적을수록 우선
            .ThenBy(c => (int)c.unit)                  // (선택) 유닛 ID 순 tie-break
            .FirstOrDefault();

        return best == default ? Skill.NoSkill : best.skill;
    }

    public UnitId PickUnitToUpgrade(AiBlackboard blackboard)
    {
        var policy = blackboard.Policy;
        var units = blackboard.MyPlayer.CurrentUnitIds.Where(id => id != (int)UnitId.UnknownUnit).ToArray();
        var dict = DataManager.UnitDict;
        if (units.Length == 0) return UnitId.UnknownUnit;
        
        // 진화 가능 유닛(3레벨 미만)
        var upgradables = units.Where(unitId => (int)unitId % 100 % 3 != 0).ToList();
        if (upgradables.Count == 0) return UnitId.UnknownUnit;
        
        // Supporter 진화 확률(상황 무관 7~15%) - 확률은 매 결정 시 살짝 흔들리도록 Uniform[MIN, MAX]
        double supporterProb = policy.SupporterProbMin +
                               (policy.SupporterProbMax - policy.SupporterProbMin) * _random.NextDouble();
        if (_random.NextDouble() < supporterProb)
        {
            var supporters = units.Where(id => dict[(int)id].UnitRole == Role.Supporter).ToList();
            if (supporters.Count > 0)
            {
                return supporters[_random.Next(supporters.Count)];
            }
        }
        
        // 각 유닛에 대한 “결정 점수 + 소량 지터” 계산
        var scoreByUnit = new Dictionary<UnitId, double>(upgradables.Count);
        foreach (var id in upgradables)
        {
            var unitId = id;
            double score = ComputeUnitUpgradeScore(unitId, blackboard);
            score += policy.JitterScale * _random.NextDouble();
            scoreByUnit[unitId] = Math.Max(1e-9, score);
        }
        
        // 확률적 선택
        return SampleBySoftmax(scoreByUnit, policy.TempSoftmax, policy.EpsilonExplore);
    }
    
    public void Ai_SpawnTower(UnitId towerId, PositionInfo pos, int cost, Player player)
    {
        var tower = SpawnTower(towerId, pos, player);
        SpawnEffect(EffectId.Upgrade, tower, tower);
        GameInfo.SheepResource -= cost;
        if (GameInfo.UrgentSpawn) GameInfo.UrgentSpawn = false;
    }
    
    public void Ai_SpawnStatue(UnitId monsterId, PositionInfo pos, int cost, Player player)
    {
        var statue = SpawnMonsterStatue(monsterId, pos, player);
        SpawnEffect(EffectId.Upgrade, statue, statue);
        GameInfo.WolfResource -= cost;
    }

    public void Ai_SpawnSheep(int cost, Player player)
    {
        GameInfo.SheepResource -= cost;
        SpawnSheep(player);
    }
    
    public void Ai_UpgradeSkill(Skill skill, int cost, Player player)
    {
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
    
    public void Ai_UpgradeUnit(UnitId prevUnitId, int cost, Player player)
    {
        DataManager.UnitDict.TryGetValue((int)prevUnitId, out var unitData);
        if (unitData == null) return;
        
        UpdateRemainSkills(player, prevUnitId);
        if (player.Faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
            UpgradeTowers(prevUnitId, player);
        }
        else if (player.Faction == Faction.Wolf)
        {
            GameInfo.WolfResource -= cost;
            UpgradeStatues(prevUnitId, player);
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

    public void Ai_RepairPortal(int cost)
    {
        GameInfo.WolfResource -= cost;
        if (_portal == null) return;
        _portal.Hp = _portal.MaxHp;
        Broadcast(new S_ChangeHp { ObjectId = _portal.Id, Hp = _portal.Hp });
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

    public void Ai_UpgradeEnchant(int cost)
    {
        if (Enchant is not { EnchantLevel: < 5 }) return;
        Console.WriteLine(Enchant.EnchantLevel);
        GameInfo.WolfResource -= cost;
        Enchant.EnchantLevel++;
    }

    public void Ai_UpgradeEconomy(int cost, Faction faction)
    {
        if (faction == Faction.Sheep)
        {
            GameInfo.SheepResource -= cost;
            GameInfo.SheepYieldParam *= 1.3f;
            GameInfo.SheepYieldUpgradeCost = (int)(GameInfo.SheepYieldUpgradeCost * 1.5f);
        }
        else
        {
            GameInfo.WolfResource -= cost;
            GameInfo.WolfYieldParam *= 1.3f;
            GameInfo.WolfYieldUpgradeCost = (int)(GameInfo.WolfYieldUpgradeCost * 1.5f);
        }
    }
}