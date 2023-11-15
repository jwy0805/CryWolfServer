using Google.Protobuf.Protocol;

namespace Server.Game;

public class StateBurn : Effect
{
    public override void Init()
    {
        if (Room == null) return;
        EffectId = EffectId.StateBurn;
    }
    
    protected override void SetEffectEffect()
    {
        IsHit = true;
        S_Despawn despawnPacket = new S_Despawn();
        despawnPacket.ObjectIds.Add(Id);
        Room?.Broadcast(despawnPacket);
    }
}