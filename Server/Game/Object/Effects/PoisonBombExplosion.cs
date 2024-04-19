using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBombExplosion : Effect
{
    protected override void SetEffectEffect()
    {
        if (Parent is SnowBomb snowBomb) snowBomb.SetEffectEffect();
        base.SetEffectEffect();
    }
}