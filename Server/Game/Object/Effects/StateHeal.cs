using Google.Protobuf.Protocol;

namespace Server.Game;

public class StateHeal : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.StateHeal;
    }
    
    protected override void SetEffectEffect()
    {
        IsHit = true;
        S_Despawn despawnPacket = new S_Despawn();
        despawnPacket.ObjectIds.Add(Id);
        Room?.Broadcast(despawnPacket);
    }
}