using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class PouchGreen: Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.PouchGreen;
    }
}