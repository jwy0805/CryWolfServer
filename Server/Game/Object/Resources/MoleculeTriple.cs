using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class MoleculeTriple : Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.MoleculeTriple;
    }
}