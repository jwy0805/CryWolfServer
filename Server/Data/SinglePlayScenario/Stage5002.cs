using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5002 : Stage
{
    private readonly Dictionary<string, Tower> _towers = new();
    private bool _finishMove = false;

    public override void Spawn(int round)
    {
        var npc = Room?.FindPlayer(go => go is Player { IsNpc: true });
        if (Room == null || npc == null) return;
        if (Room.GameInfo.FenceStartPos.Z >= 10 && _finishMove == false)
        {
            FinishMove();
            _finishMove = true;
        }
    }

    private void FinishMove()
    {
        if (Room == null) return;
        Room.SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(0, 6, 2));
        Room.SpawnTowerOnRelativeZ(UnitId.Toadstool, new Vector3(-1.5f, 6, 2));
    }
}