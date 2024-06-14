using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    public EffectId EffectId { get; set; }
    public long Duration { get; set; } = 2000;
    public bool PacketReceived { get; set; } = false;
    protected bool IsHit = false;
    
    protected readonly Scheduler Scheduler = new();

    protected Effect()
    {
        ObjectType = GameObjectType.Effect;
    }

    public override void Init()
    {
        base.Init();
        DestroyEffect(Duration);
    }
    
    private async void DestroyEffect(long destroyTime)
    {
        await Scheduler.ScheduleEvent(destroyTime, () =>
        {
            Room?.LeaveGame(Id);
        });
    }
    
    public override void Update()
    {   // Update Cycle 중 Effect 효과는 원하는 시점에서 한 번만 발동되도록 설정, 원하는 시점은 Client의 Controller에서 설정
        base.Update();
        // if (IsHit == false && PacketReceived) SetEffectEffect();
    }

    protected virtual async void EffectImpact(long impactTime)
    {
        if (Room == null) return; // Effect는 Target이 없는 경우도 있음
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            if (Parent is Creature creature) creature.ApplyEffectEffect();
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
    
    protected virtual void SetEffectEffect()
    {
        // IsHit = true;
        // Room?.LeaveGame(Id);
    }
}