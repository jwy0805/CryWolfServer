using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    public EffectId EffectId { get; set; }
    public bool PacketReceived { get; set; } = false;
    protected bool IsHit = false;

    protected Effect()
    {
        ObjectType = GameObjectType.Effect;
    }

    public override void Update()
    {
        base.Update();
        if (IsHit == false && PacketReceived) SetEffectEffect();
    }

    protected virtual void SetEffectEffect()
    {
        IsHit = true;
        Room?.LeaveGame(Id);
    }

    public virtual PositionInfo SetEffectPos(GameObject parent)
    {
        Target = parent.Target;
        return Target != null ? Target.PosInfo : parent.PosInfo;
    }
}