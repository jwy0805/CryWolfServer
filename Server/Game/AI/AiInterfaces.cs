namespace Server.Game.AI;

public interface IHeuristicsService
{
    GameRoom Room { get; }
    double EvaluatePressure(AiBlackboard blackboard);
    double EvaluateResource(AiBlackboard blackboard, int cost);
    double ComparePopulation(AiBlackboard blackboard, AiPolicy policy);
    double EvaluatePopulation(AiBlackboard blackboard, AiPolicy policy);
    double CompareValue(AiBlackboard blackboard, AiPolicy policy);
    double NeedEconomicUpgrade(AiBlackboard blackboard, AiPolicy policy);
    double VerifyCapacity(AiBlackboard blackboard, AiPolicy policy);
}

public interface IAiAction
{
    double Score();
    void Execute(GameRoom room);
}

public interface IActionFactory
{
    IEnumerable<IAiAction> Enumerate(AiBlackboard blackboard, GameRoom room);
    IAiAction CreateIdleAction(AiBlackboard blackboard);
}