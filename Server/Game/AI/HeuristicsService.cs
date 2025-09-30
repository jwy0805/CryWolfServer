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

    public float ComparePopulation(AiBlackboard blackboard)
    {
        var playerMaxPop = blackboard.PlayerMaxPopulation;
        var playerPop = blackboard.PlayerPopulation;
        var npcMaxPop = blackboard.NpcMaxPopulation;
        var npcPop = blackboard.NpcPopulation;
        var maxPopDiff = playerMaxPop - npcMaxPop;
        
        float populationScore = 0f;
        if (maxPopDiff > blackboard.PopDiffThreshold)
        {
            populationScore += 0.5f;
        }
        else if (maxPopDiff > 0 && maxPopDiff < blackboard.PopDiffThreshold)
        {
            populationScore += 0.2f;
        }

        if (npcPop == 0) npcPop = 1;
        populationScore += (float)Math.Pow((playerPop / (float)npcPop), 2) / (float)Math.PI;
        
        return populationScore;
    }

    public float EvaluatePopulation(AiBlackboard blackboard)
    {
        var npcMaxPop = blackboard.NpcMaxPopulation;
        var npcPop = blackboard.NpcPopulation;
        if (npcPop == 0) npcPop = 1;

        return (float)Math.Sqrt(npcMaxPop / (float)npcPop);
    }
    
    public float EvaluateResource(AiBlackboard blackboard, int actionCost)
    {
        if (actionCost > blackboard.Resource) return -10000f;

        return 0;
    }

    public float NeedEconomicUpgrade(AiBlackboard blackboard)
    {
        return Room.CalcEconomicScore(blackboard);
    }
}