using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage1003 : Stage
{
    private readonly Dictionary<int, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null) return;

        switch (round)
        {
            
        }
    }
}