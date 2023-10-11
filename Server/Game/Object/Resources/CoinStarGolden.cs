using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class CoinStarGolden: Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.CoinStarGolden;
        ResourceNum = (int)ResourceId;
    }
}