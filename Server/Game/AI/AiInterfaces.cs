namespace Server.Game.AI;

public interface IHeuristicsService
{
    GameRoom Room { get; }
    float EvaluatePressure(AiBlackboard blackboard);
    float EvaluateResource(AiBlackboard blackboard, int cost);
    float ComparePopulation(AiBlackboard blackboard, AiPolicy policy);
    float EvaluatePopulation(AiBlackboard blackboard);
    float NeedEconomicUpgrade(AiBlackboard blackboard);
}

public interface IAiAction
{
    float Score(AiBlackboard blackboard);
    void Execute(GameRoom room);
}

public interface IActionFactory
{
    IEnumerable<IAiAction> Enumerate(AiBlackboard blackboard, GameRoom room);
}