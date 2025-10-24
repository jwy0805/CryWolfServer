using Google.Protobuf.Protocol;
using Server.Game.AI;

namespace Server.Game;

public partial class GameRoom
{
    public double CalcEconomicScore(AiBlackboard blackboard, AiPolicy policy, double min = -5, double max = 5)
    {
        double score = 0f;
        if (blackboard.MyFaction == Faction.Sheep)
        {
            if (_round > 0)
            {
                if (_monsters.Count > 0) return score;
                score += blackboard.RoundTimeLeft * policy.RoundTimeLeftFactor;
                return score;   
            }
        }
        else // value가 앞서고 있다면 -5, 5 사이의 난수를 정규분포 확률에 따라 score로 취득 
        {
            var myValue = _statues.Values.Select(statue => GetUnitValue((int)statue.UnitId, Faction.Wolf)).Sum();
            var enemyValue = _towers.Values.Select(tower => GetUnitValue((int)tower.UnitId, Faction.Sheep)).Sum();

            if (myValue > enemyValue)
            {
                score += policy.CalcEconomicUpgrade(min, max);
            }
        }

        return score;
    }

    public double FenceHealthScore(Faction myFaction)
    {
        if (myFaction == Faction.Wolf) return -10000;
        if (_fences.Values.Any(fence => fence.Hp * 2 < fence.MaxHp)) return 3.5;
        if (_fences.Values.Any(fence => fence.Hp * 4 < fence.MaxHp)) return 7;
        return 0f;
    }

    public double StatueHealthScore(Faction myFaction)
    {
        if (myFaction == Faction.Sheep) return -10000;
        if (_statues.Values.Any(statue => statue.Hp * 2 < statue.MaxHp)) return 3.5;
        if (_statues.Values.Any(statue => statue.Hp * 4 < statue.MaxHp)) return 7;
        return 0f;
    }

    public double PortalHealthScore()
    {
        if (_portal == null) return -10000;
        if (_portal.Hp * 2 < _portal.MaxHp) return 7;
        return 0;
    }
    
    public double UpgradeStorageScore(AiBlackboard blackboard)
    {
        if (blackboard.MyFaction == Faction.Wolf || _storage == null) return -10000;
        return UpgradeBaseScore(blackboard);
    }

    public double UpgradePortalScore(AiBlackboard blackboard)
    {
        if (blackboard.MyFaction == Faction.Sheep || _portal == null) return -10000;
        return UpgradeBaseScore(blackboard);
    }

    private double UpgradeBaseScore(AiBlackboard blackboard)
    {
        double score = 0;
        
        // 1) 상대와 나의 인구수 차이에 따른 점수
        switch (blackboard.EnemyMaxPop - blackboard.MyMaxPop)
        {
            case > 8:
                score += 3f + _random.NextDouble() * 3.5;
                break;
            case > 0:
                score += _random.NextDouble() * 2;
                break;
        }
        
        // 2) 3단계 진화가 필요한 경우
        if (blackboard.MyBaseLevel == 1)
        {
            int tier2Count = blackboard.MyUnits.Count(u => (int)u % 100 % 3 == 2);
            double addScore = tier2Count >= 3 ? 1.25 : 1.0;
            
            score += blackboard.MyUnits.Where(myUnit => (int)myUnit % 100 % 3 == 2).Sum(_ => addScore);
        }

        // 3) 인구수가 최대치에 도달한 경우
        if (blackboard.MyPop == blackboard.MyMaxPop)
        {
            score += _random.NextDouble() * 4;
        }
        
        return score;
    }

    public double UpgradeEnchantScore()
    {
        return Util.Util.GetRandomValueByGaussian(_random, 0, 5, 2, 3);
    }
}