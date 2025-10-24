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

    public HashSet<SkillData> AiSkills { get; set; } = new();
    public Dictionary<UnitId, Skill> MainSkills { get; set; } = new();
    
    public void Update(GameRoom room, AiBlackboard blackboard)
    {
        var actionList = _factory.Enumerate(blackboard, room).Take(20).ToList();
        IAiAction? bestAction = null;
        double bestScore = float.NegativeInfinity;
        foreach (var action in actionList)
        {
            double score = action.Score();
            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }
        
        if (bestAction == null) return;
        if (bestScore <= Policy.IdleThreshold)
        {
            room.Push(_factory.CreateIdleAction(blackboard).Execute, room);
            Console.WriteLine($"Round {room.Round} : {room.RoundTime} - {blackboard.MyFaction}'s action -> Idle({bestAction.GetType().Name}) (score: {bestScore} / {actionList.Count}) Resource: {blackboard.MyResource}");
        }
        else
        {
            room.Push(bestAction.Execute, room);
            Console.WriteLine($"Round {room.Round} : {room.RoundTime} - {blackboard.MyFaction}'s action -> {bestAction.GetType().Name} (score: {bestScore} / {actionList.Count}) Resource: {blackboard.MyResource}");
        }
    }
}