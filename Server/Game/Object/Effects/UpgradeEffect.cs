using Google.Protobuf.Protocol;

namespace Server.Game;

public class UpgradeEffect : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.Upgrade;
    }
}