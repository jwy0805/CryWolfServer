using Google.Protobuf.Protocol;

namespace Server.Game;

public class Upgrade : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.Upgrade;
    }
}