using Google.Protobuf.Protocol;

namespace Server.Game;

public class HolyAura : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.HolyAura;
    }
    
    protected override void SetEffectEffect()
    {
        IsHit = true;
        Room?.Broadcast(new S_LeaveGame { ObjectId = Id });
    }
}