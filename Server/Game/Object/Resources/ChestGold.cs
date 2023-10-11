using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class ChestGold: Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.ChestGold;
        ResourceNum = (int)ResourceId;
    }
}