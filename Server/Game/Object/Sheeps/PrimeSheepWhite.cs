using Google.Protobuf.Protocol;

namespace Server.Game;

public class PrimeSheepWhite : Sheep
{
    public override void Init()
    {
        base.Init();
        SheepBoundMargin = 3f;
    }
}