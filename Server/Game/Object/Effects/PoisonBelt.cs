using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBelt : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(1000);
    }
}