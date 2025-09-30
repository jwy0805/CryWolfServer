using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class StageTutorialSheep : Stage
{
    private readonly Dictionary<int, MonsterStatue> _statues = new();

    public override void Spawn(int round)
    {
        var npc = Room?.Npc;
        if (Room == null || npc == null)
        {
            Console.WriteLine("Room or Npc is null");
            return;
        }

        switch (round)
        {
            case 0:
                var statue0 = Room.SpawnStatue(UnitId.DogPup, new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 13 });
                var statue1 = Room.SpawnStatue(UnitId.WolfPup, new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 13 });
                _statues.Add(0, statue0);
                _statues.Add(1, statue1);
                break;
            case 1:
                var statue2 = Room.SpawnStatue(UnitId.Snakelet, new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 14.5f });
                _statues.Add(2, statue2);
                break;
            case 3:
                var statue3 = Room.SpawnStatue(UnitId.Lurker, new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 14.5f });
                _statues.Add(3, statue3);
                break;
            case 5: 
                Room.UpgradeUnit(_statues[1], npc);
                break;
            case 6:
                Room.UpgradeUnit(_statues[3], npc);
                break;
        }

        Room.TutorialSpawnFlag = true;
    }
}