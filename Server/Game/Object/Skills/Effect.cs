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

    public override void Update()
    {
        
    }

    public virtual void SetEffectEffect(GameObject master) { }

    public virtual PositionInfo SetEffectPos(GameObject parent)
    {
        Target = parent.Target;
        return Target != null ? Target.PosInfo : parent.PosInfo;
    }
}