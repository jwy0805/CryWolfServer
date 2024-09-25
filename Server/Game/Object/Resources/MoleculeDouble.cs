using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class MoleculeDouble : Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.MoleculeDouble;
    }
}