using Google.Protobuf.Protocol;

namespace Server.Game;

public class StateCurse : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.StateCurse;
    }
    
    protected override void SetEffectEffect()
    {
        IsHit = true;
        Room?.Broadcast(new S_LeaveGame { ObjectId = Id });
    }
}