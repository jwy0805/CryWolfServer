using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class Dna : Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.Dna;
    }
}