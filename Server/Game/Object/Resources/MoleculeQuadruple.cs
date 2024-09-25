using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class MoleculeQuadruple : Resource
{
    public override void Init()
    {
        base.Init();
        ResourceId = ResourceId.MoleculeQuadruple;
    }
}