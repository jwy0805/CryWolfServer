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

    public override void Init()
    {
        base.Init();
    }
    
    public override void Update()
    {   // Update Cycle 중 Effect 효과는 원하는 시점에서 한 번만 발동되도록 설정, 원하는 시점은 Client의 Controller에서 설정
        base.Update();
        if (IsHit == false && PacketReceived) SetEffectEffect();
    }

    protected virtual void SetEffectEffect()
    {
        IsHit = true;
        Room?.LeaveGame(Id);
    }

    public virtual PositionInfo SetEffectPos(GameObject parent, PositionInfo? posInfo = null)
    {
        return posInfo ?? parent.PosInfo;
    }
}