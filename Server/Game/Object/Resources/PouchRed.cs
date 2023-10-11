using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class PouchRed: Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.PouchRed;
        ResourceNum = (int)ResourceId;
    }
}