using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class StageTutorialWolf : Stage
{
    private readonly Dictionary<float, Tower> _towers = new();

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
                var tower0 = Room.SpawnTowerOnRelativeZ(UnitId.PracticeDummy, new Vector3(0, 6, 1));
                var tower1 = Room.SpawnTowerOnRelativeZ(UnitId.Bloom, new Vector3(1f, 6, -0.5f));
                var tower2 = Room.SpawnTowerOnRelativeZ(UnitId.Bloom, new Vector3(-1f, 6, -0.5f));
                _towers.Add(0, tower0);
                _towers.Add(1, tower1);
                _towers.Add(2, tower2);
                break;
            
            case 1:
                if (Room.RoundTime >= 8) return;
                Room.Broadcast(new S_StepTutorial { Process = false });
                break;
            
            case 3:
                if (Room.RoundTime >= 22) return;
                Room.Broadcast(new S_StepTutorial { Process = false });
                break;
        }

        Room.TutorialFlag = true;
    }
}