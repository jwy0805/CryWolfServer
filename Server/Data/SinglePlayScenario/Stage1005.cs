using Google.Protobuf.Protocol;
using Newtonsoft.Json.Serialization;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1005 : Stage
{
    private readonly Dictionary<string, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.FindPlayer(go => go is Player { IsNpc: true });
        if (Room == null || npc == null) return;
    }
}