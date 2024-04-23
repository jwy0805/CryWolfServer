using Google.Protobuf.Protocol;

namespace Server.Game;

public class SkeletonGiantRevive : Effect
{
    public override void Init()
    {
        base.Init();
        EffectId = EffectId.SkeletonGiantRevive;
    }
}