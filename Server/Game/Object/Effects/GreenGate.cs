using Google.Protobuf.Protocol;

namespace Server.Game;

public class GreenGate : Effect
{
    public override void Init()
    {
        base.Init();
        EffectImpact(2000);
    }
    
    protected override async void EffectImpact(long impactTime)
    {
        if (Room == null) return; // Effect는 Target이 없는 경우도 있음
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            if (Parent is Creature creature) creature.ApplyEffectEffect(EffectId);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}