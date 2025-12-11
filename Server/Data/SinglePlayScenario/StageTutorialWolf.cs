using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class StageTutorialWolf : Stage
{
    private readonly Dictionary<float, Tower> _towers = new();

    public override void Spawn(int round)
    {
        var npc = Room?.FindPlayer(go => go is Player { IsNpc: true });
        if (Room == null || npc == null)
        {
            Console.WriteLine("Room or Npc is null");
            return;
        }

        switch (round)
        {
            case 0:
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.TargetDummy, new Vector3(0, 6, 1.5f));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.Bloom, new Vector3(1f, 6, -0.5f));
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.Bloom, new Vector3(-1f, 6, -0.5f));
                var tower3 = Room.SpawnTowerOnRelativeZ(UnitId.Rabbit, new Vector3(1f, 6, 0.75f));
                _towers.Add(0, tower0);
                _towers.Add(1, tower1);
                _towers.Add(2, tower2);
                _towers.Add(3, tower3);
                break;
        }

        Room.TutorialSpawnFlag = true;
    }
}