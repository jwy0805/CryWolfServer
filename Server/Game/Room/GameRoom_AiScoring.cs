using Google.Protobuf.Protocol;
using Server.Game.AI;

namespace Server.Game;

public partial class GameRoom
{
    public float CalcEconomicScore(AiBlackboard blackboard, double min = -5, double max = 5)
    {
        var policy = blackboard.Policy;
        float score = 0f;
        if (blackboard.MyFaction == Faction.Sheep)
        {
            if (_monsters.Count > 0) return score;
            score += blackboard.RoundTimeLeft * policy.RoundTimeLeftFactor;
            return score;   
        }
        else // value가 앞서고 있다면 -5, 5 사이의 난수를 정규분포 확률에 따라 score로 취득 
        {
            var myValue = _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId, Faction.Wolf)).Sum();
            var enemyValue = _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId, Faction.Sheep)).Sum();

            if (myValue > enemyValue)
            {
                score += (float)Util.Util.GetRandomValueByGaussian(_random, min, max, 0, 1.5f);
            }
        }

        return score;
    }

    public float FenceHealthScore(Faction myFaction)
    {
        if (myFaction == Faction.Wolf) return -10000f;
        if (_fences.Values.Any(fence => fence.Hp * 2 < fence.MaxHp)) return 3.5f;
        if (_fences.Values.Any(fence => fence.Hp * 4 < fence.MaxHp)) return 7f;
        return 0.5f;
    }

    public float StatueHealthScore(Faction myFaction)
    {
        if (myFaction == Faction.Sheep) return -10000f;
        if (_statues.Values.Any(statue => statue.Hp * 2 < statue.MaxHp)) return 3.5f;
        if (_statues.Values.Any(statue => statue.Hp * 4 < statue.MaxHp)) return 7f;
        return 0.5f;
    }

    public float PortalHealthScore()
    {
        if (_portal == null) return -10000f;
        if (_portal.Hp * 2 < _portal.MaxHp) return 7f;
        return 0f;
    }
    
    public float UpgradeStorageScore(AiBlackboard blackboard)
    {
        if (blackboard.MyFaction == Faction.Wolf || _storage == null) return -10000f;
        return UpgradeBaseScore(blackboard);
    }

    public float UpgradePortalScore(AiBlackboard blackboard)
    {
        if (blackboard.MyFaction == Faction.Sheep || _portal == null) return -10000f;
        return UpgradeBaseScore(blackboard);
    }

    private float UpgradeBaseScore(AiBlackboard blackboard)
    {
        float score = 0;
        
        // 1) 상대와 나의 인구수 차이에 따른 점수
        switch (blackboard.EnemyMaxPop - blackboard.MyMaxPop)
        {
            case > 8:
                score += 3f + (float)_random.NextDouble() * 3.5f;
                break;
            case > 0:
                score += (float)_random.NextDouble() * 2f;
                break;
        }
        
        // 2) 3단계 진화가 필요한 경우
        if (blackboard.MyBaseLevel == 1)
        {
            int tier2Count = blackboard.MyUnits.Count(u => (int)u % 100 % 3 == 2);
            float addScore = tier2Count > 3 ? 1.25f : 1.0f;

            score += blackboard.MyUnits.Where(myUnit => (int)myUnit % 100 % 3 == 2).Sum(_ => addScore);
        }

        // 3) 인구수가 최대치에 도달한 경우
        if (blackboard.MyPop == blackboard.MyMaxPop)
        {
            score += (float)_random.NextDouble() * 2f;
        }
        
        return score;
    }

    public float UpgradeEnchantScore()
    {
        return (float)Util.Util.GetRandomValueByGaussian(_random, 0, 6, 2, 1.5);
    }
}