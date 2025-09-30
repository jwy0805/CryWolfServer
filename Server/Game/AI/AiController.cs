namespace Server.Game.AI;

public class AiController
{
    private readonly IActionFactory _factory;
    private readonly AiPolicy _policy;

    public AiController(IActionFactory factory, AiPolicy policy)
    {
        _factory = factory;
        _policy = policy;
    }

    public int FenceDamageThisRound { get; set; } = 0;
    public int SheepDamageThisRound { get; set; } = 0;
    public bool FenceMovedThisRound { get; set; } = false;
    
    public void Update(GameRoom room, AiBlackboard blackboard)
    {
        var actionList = _factory.Enumerate(blackboard, room).Take(20).ToList();
        IAiAction? bestAction = null;
        float bestScore = float.NegativeInfinity;
        foreach (var action in actionList)
        {
            float score = action.Score(blackboard);
            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        if (bestAction != null && bestScore > 0.01f)
        {
            room.Push(bestAction.Execute, room);
        }
    }
}