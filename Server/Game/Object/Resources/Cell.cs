using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class Cell : Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.Cell;   
    }
}