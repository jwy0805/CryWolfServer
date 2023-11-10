using Google.Protobuf.Protocol;

namespace Server.Game;

public class StateAggro : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.StateAggro;
    }
    
    protected override void SetEffectEffect()
    {
        IsHit = true;
        Room?.Broadcast(new S_LeaveGame { ObjectId = Id });
    }
}