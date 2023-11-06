using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    public EffectId EffectId;
    public bool PacketReceived { get; set; } = false;

    protected Effect()
    {
        ObjectType = GameObjectType.Effect;
    }

    public virtual void SetEffectEffect()
    {
        Room?.LeaveGame(Id);
    }

    public virtual PositionInfo SetEffectPos(GameObject parent)
    {
        Target = parent.Target;
        return Target != null ? Target.PosInfo : parent.PosInfo;
    }
}