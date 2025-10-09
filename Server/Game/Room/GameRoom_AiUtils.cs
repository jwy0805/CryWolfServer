using System.Numerics;
using Google.Protobuf.Protocol;
using MathNet.Numerics;
using Server.Data;
using Server.Game.AI;

namespace Server.Game;

public partial class GameRoom
{
    private struct UnitCandidate
    {
        public UnitId Id;
        public Role Role;
        public int AttackType;
    }
    
    public UnitId[] GetAiDeck()
    {
        var candidatesPool = DataManager.UnitDict.Keys.Select(k => (UnitId)k)
            .Where(id => id != UnitId.UnknownUnit)
            .Where(id => ((int)id % 100) % 3 == 0).ToList();

        var candidates = candidatesPool.Select(id => new UnitCandidate
        {
            Id = id,
            Role = DataManager.UnitDict[(int)id].unitRole,
            AttackType = DataManager.UnitDict[(int)id].stat.AttackType
        }).ToList();
        
        bool IsMelee(Role r) => r is Role.Warrior or Role.Tanker;
        bool IsRanged(Role r) => r is Role.Ranger or Role.Mage;
        
        var melee = candidates.Where(c => IsMelee(c.Role)).ToList();
        var ranged = candidates.Where(c => IsRanged(c.Role)).ToList();
        var supporter = candidates.Where(c => c.Role == Role.Supporter).ToList();
        var airAttackers = candidates.Where(c => 
            (AttackType)c.AttackType is AttackType.Air or AttackType.Both).ToList();
        var deck = new List<UnitId>(6);
        var excludes = new HashSet<UnitId>();
        
        // Helpers
        UnitCandidate? PickRandom(IReadOnlyList<UnitCandidate> list) 
            => list.Count == 0 ? null : list.ElementAt(_random.Next(list.Count));

        UnitCandidate? PickRandomExcept(IReadOnlyList<UnitCandidate> list, HashSet<UnitId> exclude)
        {
            var filtered = list.Where(uc => !exclude.Contains(uc.Id)).ToList();
            return filtered.Count == 0 ? null : filtered.ElementAt(_random.Next(filtered.Count));
        }

        // Guaranteed air attack
        var airPick = PickRandom(airAttackers);
        if (airPick.HasValue)
        {
            deck.Add(airPick.Value.Id);
            excludes.Add(airPick.Value.Id);
        }
        
        // Balanced melee/ranged
        var meleePick = PickRandomExcept(melee, excludes);
        if (meleePick.HasValue)
        {
            deck.Add(meleePick.Value.Id);
            excludes.Add(meleePick.Value.Id);
        }

        var rangedPick = PickRandomExcept(ranged, excludes);
        if (rangedPick.HasValue)
        {
            deck.Add(rangedPick.Value.Id);
            excludes.Add(rangedPick.Value.Id);
        }
        
        // balanced remaining
        while (deck.Count < 6)
        {
            int currentMelee = deck.Count(id => IsMelee(DataManager.UnitDict[(int)id].unitRole));
            int currentRanged = deck.Count(id => IsRanged(DataManager.UnitDict[(int)id].unitRole));
            var preferPool = currentMelee < currentRanged ? melee : ranged;
            var pick = PickRandomExcept(preferPool, excludes) ?? PickRandomExcept(candidates, excludes);
            if (!pick.HasValue) break;
            deck.Add(pick.Value.Id);
            excludes.Add(pick.Value.Id);
        }

        return deck.Take(6).ToArray();
    }
    
    private float ComputeUnitProb(Faction faction)
    {
        Dictionary<Role, int> roleDict;
        if (faction == Faction.Sheep)
        {
            roleDict = _towers.Values
                .GroupBy(tower => DataManager.UnitDict[(int)tower.UnitId].unitRole)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        else
        {
            roleDict = _statues.Values
                .GroupBy(statue => DataManager.UnitDict[(int)statue.UnitId].unitRole)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        
        int meleeCount = 0;
        int rangedCount = 0;

        if (roleDict.TryGetValue(Role.Warrior, out var warriorCnt)) meleeCount += warriorCnt;
        if (roleDict.TryGetValue(Role.Tanker, out var tankerCnt)) meleeCount += tankerCnt;
        if (roleDict.TryGetValue(Role.Ranger, out var rangerCnt)) rangedCount += rangerCnt;
        if (roleDict.TryGetValue(Role.Mage, out var mageCnt)) rangedCount += mageCnt;
        
        return (meleeCount + rangedCount) > 0 ? meleeCount / (float)(meleeCount + rangedCount) : 0.5f; // 둘 다 0이면 중립값
    }
    
    // ---- 스냅샷: 아군/적군 좌표 ----
    private List<(double X, double Z)> Snapshot2D()
    {
        var list = new List<(double X, double Z)>();
        foreach (var tower in _towers.Values)
        {
            var vec = tower.CellPos;
            list.Add((vec.X, vec.Z));
        }

        return list;
    }

    private List<double> SnapshotStatueX()
    {
        var list = new List<double>();
        foreach (var statue in _statues.Values)
        {
            var vec = statue.CellPos;
            list.Add(vec.X);
        }

        return list;
    }
    
    // ---- 바이어스 1: 센터(x=0) 가우시안 (로그가중치) ----
    private double CenterLogWeight(double x, double sigmaCenter, double centerExp)
    {
        double t = x / sigmaCenter;
        return -0.5 * centerExp * t * t;
    }
    
    // ---- 고정 z에서 x를 바이어스로 선택(최댓값 픽) ----
    private (float bestX, double bestLogW, double minTowerDist) SampleXWithWeight(double z, double sigmaCenter,
        double centerExp, float xMin, float xMax, int nX, IReadOnlyList<(double X, double Z)> towersXZ,
        IReadOnlyList<double> statueXs, double distMinimum = 1.0, double sigmaRepulsion = 0.25,
        double betaTowardsCenter = 0.75, double sigmaStatue = 1.5)
    {
        double bestLogWeight = double.NegativeInfinity;
        float bestX = 0;
        double bestMinDist = double.PositiveInfinity;
        double inv2SigmaSquaredStatue = 1.0 / (2.0 * sigmaStatue * sigmaStatue);
        
        for (int i = 0; i < nX; i++)
        {
            float x = (float)(xMin + (xMax - xMin) * _random.NextDouble());
            
            // 1) 센터 바이어스 (로그)
            double logWeight = CenterLogWeight(x, sigmaCenter, centerExp);
            // 2) 아군과 너무 가깝지 않게 (최소거리 기반, distMinimum 미만만 패널티)
            double minDistFromTower = double.PositiveInfinity;
            for (int t = 0; t < towersXZ.Count; t++)
            {
                var (towerX, towerZ) = towersXZ[t];
                double d = Util.Util.DistanceXZ(x, z, towerX, towerZ);
                if (d < minDistFromTower) minDistFromTower = d;
            }

            if (minDistFromTower < distMinimum)
            {
                double normalizedDistFromSafe = (minDistFromTower - distMinimum) / sigmaRepulsion; // 음수
                logWeight += -0.5 * normalizedDistFromSafe * normalizedDistFromSafe;
            }
            
            // 3) 적 타워 X 쪽으로 끌리되 0으로 약간 압축(혼합 가우시안 평균의 log)
            if (statueXs.Count > 0)
            {
                double sum = 0.0;
                for (int s = 0; s < statueXs.Count; s++)
                {
                    double mu = betaTowardsCenter * statueXs[s];
                    double dx = x - mu;
                    sum += Math.Exp(-dx * dx * inv2SigmaSquaredStatue);
                }
                double avg = sum / statueXs.Count;
                if (avg < 1e-12) avg = 1e-12;
                logWeight += Math.Log(avg);
            }

            // 최댓값 픽
            if (logWeight > bestLogWeight)
            {
                bestLogWeight = logWeight;
                bestX = x;
                bestMinDist = minDistFromTower;
            }
        }
        
        return (bestX, bestLogWeight, bestMinDist);
    }
    
    // ----------------- 시그마 어댑티브 -----------------
    private double ComputeAdaptiveCenterSigma(double baseSigma, double k = 0.1)
    {
        if (_towers.Count == 0) return baseSigma;
        double avgAbsX = _towers.Values.Average(s => Math.Abs(s.CellPos.X));
        double sigmaEff = baseSigma / Math.Max(1e-6, 1.0 + k * avgAbsX);
        return Math.Max(0.25, sigmaEff);
    }
    
    // ---- z만 균등으로 샘플 ----
    private Vector3 SampleBaseCandidate(bool frontliner)
    {
        var minZBase = frontliner ? GameInfo.FenceStartPos.Z + 0.5f : GameInfo.FenceStartPos.Z - 1.5f;
        var maxZBase = frontliner ? GameInfo.FenceStartPos.Z + 1.5f : GameInfo.FenceStartPos.Z - 0.5f;

        float z;
        if (GameInfo.SheepDamageThisRound <= 0) 
        {
            // Sheep 피해가 없으면 그대로 균등분포
            z = (float)(minZBase + (maxZBase - minZBase) * _random.NextDouble());
        }
        else
        {
            // Sheep 피해가 존재할 때만 FenceCenter로 bias
            var totalSheepHp = _sheeps.Values.Select(sheep => sheep.Stat.MaxHp).Sum();
            var totalFenceHp = DataManager.FenceDict[_storage?.Level ?? 1].stat.MaxHp * GameInfo.NorthMaxFenceCnt;
            double sheepFactor = Math.Clamp(GameInfo.SheepDamageThisRound / (double)totalSheepHp, 0.0, 1.0);
            double fenceFactor = 1.0 - Math.Clamp(GameInfo.FenceDamageThisRound / (double)totalFenceHp * 0.3, 0.0, 1.0);
            double bias = (sheepFactor + fenceFactor) * 0.5;

            float fenceCenterZ = GameInfo.FenceCenter.Z;
            float minZ = (float)(minZBase * (1 - bias) + fenceCenterZ * bias);
            float maxZ = (float)(maxZBase * (1 - bias) + fenceCenterZ * bias);

            z = (float)(minZ + (maxZ - minZ) * _random.NextDouble());
        }

        return new Vector3(0, 6, z);
    }
    
    private double GetGaussianWeight(double dist, double distThreshold = 1.5, double sigma = 0.5)
    {
        return Math.Exp(-Math.Pow(dist - distThreshold, 2) / (2 * sigma * sigma));
    }
    
        // ---- 단일 메인 스킬 기준 선택 (2단계 이내면 후보 반환) ----
    // out distToMain: 메인까지 남은 "즉시 찍을 수 있는 단계 수" 근사치
    // out upgradableNow: 반환한 pick을 지금 당장 찍을 수 있는지
    private Skill PickNextTowardsMain(Skill mainSkill, out int distToMain, out bool upgradableNow, Player player)
    {
        distToMain = int.MaxValue;
        upgradableNow = false;

        var learned = player.SkillUpgradedList;
        if (learned.Contains(mainSkill)) return Skill.NoSkill;  
        
        var requiredSkills = GetAllSkillsRequired(mainSkill, GameData.SkillTree);
        requiredSkills.Remove(Skill.NoSkill);

        var missing = requiredSkills.Where(s => !learned.Contains(s)).ToHashSet();
        if (missing.Count > 2) return Skill.NoSkill;    // 2단계 이내만 고려

        distToMain = missing.Count + 1;
        
        // 지금 당장 찍을 수 있는 스킬 먼저 업그레이드
        var now = missing
            .Where(skill => ArePrereqSatisfied(skill, learned, GameData.SkillTree)).ToList();
        if (now.Count > 0)
        {
            upgradableNow = true;
            return now[0];
        }
        
        // Main skill 찍을 수 있으면 Main skill
        if (ArePrereqSatisfied(mainSkill, learned, GameData.SkillTree))
        {
            upgradableNow = true;
            distToMain = 1;
            return mainSkill;
        }
        
        // 선행이 가장 적게 부족한 missing 스킬
        var pickByFewestNeeds = missing.OrderBy(skill => GetMissingCount(skill, player)).FirstOrDefault();
        if (!pickByFewestNeeds.Equals(default(Skill)))
        {
            upgradableNow = false;
            return pickByFewestNeeds;
        }

        return Skill.NoSkill;
    }

    private HashSet<Skill> GetAllSkillsRequired(Skill skill, Dictionary<Skill, HashSet<Skill>> tree)
    {
        var skills = new HashSet<Skill>();
        Collect(skill, skills, tree);
        return skills;

        void Collect(Skill s, HashSet<Skill> visited, Dictionary<Skill, HashSet<Skill>> t)
        {
            if (!t.TryGetValue(s, out var prerequisites)) return;
            foreach (var pre in prerequisites.Where(visited.Add))
            {
                Collect(pre, visited, t);
            }
        }
    }

    private bool ArePrereqSatisfied(Skill skill, HashSet<Skill> learned, Dictionary<Skill, HashSet<Skill>> tree)
        => !tree.TryGetValue(skill, out var prerequisites) 
           || prerequisites.All(p => p == Skill.NoSkill || learned.Contains(p));

    private int GetMissingCount(Skill s, Player player)
    {
        if (!GameData.SkillTree.TryGetValue(s, out var prerequisites) || prerequisites.Count == 0) return 0;
        return prerequisites.Count(pre => pre != Skill.NoSkill && !player.SkillUpgradedList.Contains(pre));
    }

    private UnitId SampleBySoftmax(Dictionary<UnitId, double> scores, double temperature, double epsilon)
    {
        if (scores.Count == 0) return default(UnitId);
        
        // epsilon-탐색: 가끔은 완전 랜덤
        if (_random.NextDouble() < epsilon)
        {
            var keys = scores.Keys.ToList();
            return keys[_random.Next(keys.Count)];
        }
        
        // Softmax: p_i ☆ exp(score_i / T), star: softmax 연산
        double maxScore = scores.Values.Max();
        var weights = new List<(UnitId unitId, double w)>(scores.Count);
        double sum = 0;

        foreach (var pair in scores)
        {
            double z = (pair.Value - maxScore) / Math.Max(1e-6, temperature);
            double w = Math.Exp(z);
            weights.Add((pair.Key, w));
            sum += w;
        }
        
        // 합이 0이면(혹은 극소값) 랜덤
        if (sum is <= 0 or double.NaN)
        {
            var keys = scores.Keys.ToList();
            return keys[_random.Next(keys.Count)];
        }

        double r = _random.NextDouble() * sum;
        double acc = 0;
        foreach (var (unitId, weight) in weights)
        {
            acc += weight;
            if (r <= acc) return unitId;
        }
        
        return weights.Last().unitId; // 수치오차 보정
    }

    private double ComputeUnitUpgradeScore(UnitId unitId, AiBlackboard blackboard)
    {
        var role = DataManager.UnitDict[(int)unitId].unitRole;
        double monsterOverflow = Util.Util.Clamp01(GetMonsterOverflowNorm());
        double fenceDamageNorm = Util.Util.Clamp01(GetFenceDamageNorm());
        bool fenceMoved = GetFenceMoved();

        double score = 1;
        if (blackboard.MyFaction == Faction.Sheep)
        {
            switch (role)
            {
                case Role.Ranger or Role.Mage:
                    score *= 1 + blackboard.Policy.WaveOverflowFactor * monsterOverflow;
                    break;
                case Role.Tanker or Role.Warrior:
                    score *= 1 + blackboard.Policy.FenceDamageFactor * fenceDamageNorm;
                    break;
            }
        }
        else
        {
            switch (role)
            {
                case Role.Ranger or Role.Mage:
                    score *= 1 + blackboard.Policy.FenceLowDamageFactor * (1 - fenceDamageNorm);
                    break;
                case Role.Tanker or Role.Warrior:
                    if (fenceMoved) score *= 1 + blackboard.Policy.FenceMoveFactor;
                    break;
            }
        }

        var enemyDistributionDict = GetEnemyRoleDistribution(blackboard.MyFaction);
        double affinitySum = 0;
        foreach (var (enemyRole, value) in enemyDistributionDict)
        {
            if (value <= 0) continue;
            if (GameManager.Instance.RoleAffinityMatrix.TryGetValue((role, enemyRole), out var aff))
            {
                affinitySum += value * aff;
            }
        }

        score *= Math.Max(blackboard.Policy.AffinityFloorFactor, 1 + blackboard.Policy.AffinityFactor * affinitySum);
        return score;
    }

    private double GetMonsterOverflowNorm()
    {
        var diff = _monsters.Count - GameInfo.NorthMaxMonster;
        return _monsters.Count == 0 || diff < 0 ? 0 : diff / (double)GameInfo.NorthMaxMonster;
    }

    private double GetFenceDamageNorm()
    {
        if (_storage == null) return 0;  
        var fenceMaxHp = DataManager.FenceDict[_storage.Level].stat.MaxHp;
        return GameInfo.FenceDamageThisRound / (double)(fenceMaxHp * GameInfo.NorthMaxFenceCnt);
    }

    private bool GetFenceMoved()
    {
        return GameInfo.FenceMovedThisRound;
    }

    private Dictionary<Role, double> GetEnemyRoleDistribution(Faction faction)
    {
        var enemyUnits = faction == Faction.Sheep
            ? _statues.Values.Select(statue => (statue.UnitId, DataManager.UnitDict[(int)statue.UnitId].unitRole)).ToList()
            : _towers.Values.Select(tower => (tower.UnitId, DataManager.UnitDict[(int)tower.UnitId].unitRole)).ToList();
        var total = enemyUnits.Count;
        if (total == 0)
        {
            return Enum.GetValues(typeof(Role)).Cast<Role>().ToDictionary(r => r, r => 0.0);
        }

        return enemyUnits
            .GroupBy(tuple => tuple.unitRole)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Count() / (double)total);
    }
}