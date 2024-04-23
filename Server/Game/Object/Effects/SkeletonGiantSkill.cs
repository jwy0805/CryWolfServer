using Google.Protobuf.Protocol;

namespace Server.Game;

public class SkeletonGiantSkill : Effect
{
    public override void Init()
    {
        base.Init();
        EffectId = EffectId.SkeletonGiantSkill;
    }
}