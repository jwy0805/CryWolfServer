using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.AI;

public class AiController
{
    private readonly IActionFactory _factory;
    public readonly AiPolicy Policy;

    public AiController(IActionFactory factory, AiPolicy policy)
    {
        _factory = factory;
        Policy = policy;
    }

    public UnitId[] AiUnits { get; init; } = Array.Empty<UnitId>();
    public HashSet<SkillData> AiSkills { get; set; } = new();
    public Dictionary<UnitId, Skill> MainSkills { get; set; } = new();
    
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