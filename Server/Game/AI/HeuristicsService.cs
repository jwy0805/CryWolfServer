using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public class HeuristicsService : IHeuristicsService
{
    public GameRoom Room { get; }

    public HeuristicsService(GameRoom room)
    {
        Room = room;
    }

    public double EvaluatePressure(AiBlackboard blackboard) => blackboard.TotalPressure;

    public double ComparePopulation(AiBlackboard blackboard, AiPolicy policy)
    {
        var enemyMaxPop = blackboard.EnemyMaxPop;
        var enemyPop = blackboard.EnemyPop;
        var myMaxPop = blackboard.MyMaxPop;
        var myPop = blackboard.MyPop;
        var maxPopDiff = enemyMaxPop - myMaxPop;
        
        double populationScore = 0;
        if (maxPopDiff > policy.PopDiffThreshold)
        {
            populationScore += 0.5;
        }
        else if (maxPopDiff > 0 && maxPopDiff < policy.PopDiffThreshold)
        {
            populationScore += 0.2;
        }

        if (myPop == 0) myPop = 1;
        populationScore += policy.ComparePopulation(enemyPop, myPop);
        
        return populationScore;
    }

    public double EvaluatePopulation(AiBlackboard blackboard, AiPolicy policy)
    {
        var popDiff = blackboard.MyMaxPop - blackboard.MyPop;

        return policy.EvaluatePopulation(popDiff);
    }

    public double CompareValue(AiBlackboard blackboard, AiPolicy policy)
    {
        int myValue;
        int enemyValue;
        if (blackboard.MyFaction == Faction.Sheep)
        {
            myValue = Room.GetTowerValue();
            enemyValue = Room.GetStatueValue();
        }
        else
        {
            myValue = Room.GetStatueValue();
            enemyValue = Room.GetTowerValue();
        }
        
        return policy.CompareValueForUnitUpgrade(myValue, enemyValue, blackboard.MyPop);
    }
    
    public double EvaluateResource(AiBlackboard blackboard, int actionCost)
    {
        if (actionCost > blackboard.MyResource) return -10000f;

        return 0;
    }

    public double NeedEconomicUpgrade(AiBlackboard blackboard, AiPolicy policy)
    {
        return Room.CalcEconomicScore(blackboard, policy);
    }
    
    public double VerifyCapacity(AiBlackboard blackboard, AiPolicy policy)
    {
        var popDiff = blackboard.MyMaxPop - blackboard.MyPop;
        if (popDiff <= 0)
        {
            return -10000f;
        }

        return 0;
    }
}