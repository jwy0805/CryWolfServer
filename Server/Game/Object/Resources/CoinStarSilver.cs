using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class CoinStarSilver: Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.CoinStarSilver;
        ResourceNum = (int)ResourceId;
    }
}