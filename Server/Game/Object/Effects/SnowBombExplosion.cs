using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBombExplosion : Effect
{
    protected override void SetEffectEffect()
    {
        if (Parent is SnowBomb snowBomb) snowBomb.SetEffectEffect();
        base.SetEffectEffect();
    }
}