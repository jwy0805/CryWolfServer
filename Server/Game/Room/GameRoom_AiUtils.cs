using System.Numerics;
using Google.Protobuf.Protocol;
using MathNet.Numerics;
using Server.Data;
using Server.Game.AI;

namespace Server.Game;

public partial class GameRoom
{
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
        var minZ = frontliner ? GameInfo.FenceStartPos.Z + 0.5f : GameInfo.FenceStartPos.Z - 1.5f;
        var maxZ = frontliner ? GameInfo.FenceStartPos.Z + 1.5f : GameInfo.FenceStartPos.Z - 0.5f;
        
        float z = (float)(minZ + (maxZ - minZ) * _random.NextDouble());
        return new Vector3(0, 6, z);
    }
    
    private double GetGaussianWeight(double dist, double distThreshold = 1.5, double sigma = 0.5)
    {
        return Math.Exp(-Math.Pow(dist - distThreshold, 2) / (2 * sigma * sigma));
    }
    
        // ---- 단일 메인 스킬 기준 선택 (2단계 이내면 후보 반환) ----
    // out distToMain: 메인까지 남은 "즉시 찍을 수 있는 단계 수" 근사치
    // out upgradableNow: 반환한 pick을 지금 당장 찍을 수 있는지
    private Skill PickNextTowardsMain(Skill mainSkill, out int distToMain, out bool upgradableNow)
    {
        distToMain = int.MaxValue;
        upgradableNow = false;
        if (Npc == null) return Skill.NoSkill;

        var learned = Npc.SkillUpgradedList;
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
        var pickByFewestNeeds = missing.OrderBy(GetMissingCount).FirstOrDefault();
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

    private int GetMissingCount(Skill s)
    {
        if (Npc == null) return 0;
        if (!GameData.SkillTree.TryGetValue(s, out var prerequisites) || prerequisites.Count == 0) return 0;

        return prerequisites.Count(pre => pre != Skill.NoSkill && !Npc.SkillUpgradedList.Contains(pre));
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
        if (Npc == null) return 0;
        var role = DataManager.UnitDict[(int)unitId].unitRole;
        var faction = Npc.Faction;
        double monsterOverflow = Util.Util.Clamp01(GetMonsterOverflowNorm());
        double fenceDamageNorm = Util.Util.Clamp01(GetFenceDamageNorm());
        bool fenceMoved = GetFenceMoved();

        double score = 1;
        if (faction == Faction.Sheep)
        {
            switch (role)
            {
                case Role.Ranger or Role.Mage:
                    score *= 1 + blackboard.WaveOverflowFactor * monsterOverflow;
                    break;
                case Role.Tanker or Role.Warrior:
                    score *= 1 + blackboard.FenceDamageFactor * fenceDamageNorm;
                    break;
            }
        }
        else
        {
            switch (role)
            {
                case Role.Ranger or Role.Mage:
                    score *= 1 + blackboard.FenceLowDamageFactor * (1 - fenceDamageNorm);
                    break;
                case Role.Tanker or Role.Warrior:
                    if (fenceMoved) score *= 1 + blackboard.FenceMoveFactor;
                    break;
            }
        }

        var enemyDistributionDict = GetEnemyRoleDistribution(faction);
        double affinitySum = 0;
        foreach (var (enemyRole, value) in enemyDistributionDict)
        {
            if (value <= 0) continue;
            if (GameManager.Instance.RoleAffinityMatrix.TryGetValue((role, enemyRole), out var aff))
            {
                affinitySum += value * aff;
            }
        }

        score *= Math.Max(blackboard.AffinityFloorFactor, 1 + blackboard.AffinityFactor * affinitySum);
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
        return _aiController?.FenceDamageThisRound / (fenceMaxHp * GameInfo.NorthMaxFenceCnt) ?? 0;
    }

    private bool GetFenceMoved()
    {
        return _aiController?.FenceMovedThisRound ?? false;
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