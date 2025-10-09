using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public class HeuristicsService : IHeuristicsService
{
    public GameRoom Room { get; }

    public HeuristicsService(GameRoom room)
    {
        Room = room;
    }

    public float EvaluatePressure(AiBlackboard blackboard) => blackboard.TotalPressure;

    public float ComparePopulation(AiBlackboard blackboard, AiPolicy policy)
    {
        var playerMaxPop = blackboard.EnemyMaxPop;
        var playerPop = blackboard.EnemyPop;
        var npcMaxPop = blackboard.MyMaxPop;
        var npcPop = blackboard.MyPop;
        var maxPopDiff = playerMaxPop - npcMaxPop;
        
        float populationScore = 0f;
        if (maxPopDiff > policy.PopDiffThreshold)
        {
            populationScore += 0.5f;
        }
        else if (maxPopDiff > 0 && maxPopDiff < policy.PopDiffThreshold)
        {
            populationScore += 0.2f;
        }

        if (npcPop == 0) npcPop = 1;
        populationScore += (float)Math.Pow((playerPop / (float)npcPop), 2) / (float)Math.PI;
        
        return populationScore;
    }

    public float EvaluatePopulation(AiBlackboard blackboard)
    {
        var npcMaxPop = blackboard.MyMaxPop;
        var npcPop = blackboard.MyPop;
        if (npcPop == 0) npcPop = 1;

        return (float)Math.Sqrt(npcMaxPop / (float)npcPop);
    }
    
    public float EvaluateResource(AiBlackboard blackboard, int actionCost)
    {
        if (actionCost > blackboard.MyResource) return -10000f;

        return 0;
    }

    public float NeedEconomicUpgrade(AiBlackboard blackboard)
    {
        return Room.CalcEconomicScore(blackboard);
    }
}