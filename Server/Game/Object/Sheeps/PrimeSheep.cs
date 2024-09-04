using Google.Protobuf.Protocol;

namespace Server.Game;

public class PrimeSheep : Sheep
{
    public override void Init()
    {
        base.Init();
        SheepBoundMargin = 3f;
    }
}