using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBelt : Effect
{
    public override void Update()
    {
        base.Update();
        CellPos = Parent!.CellPos;
    }
}