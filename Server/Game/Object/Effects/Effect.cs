using Google.Protobuf.Protocol;

namespace Server.Game;

public class Effect : GameObject
{
    private int _killLog;

    protected readonly Scheduler Scheduler = new();
    
    public EffectId EffectId { get; set; }
    public long Duration { get; set; } = 2000;
    public bool PacketReceived { get; set; } = false;

    public override int KillLog
    {
        get => _killLog;
        set
        {
            _killLog = value;
            if (Parent != null) Parent.KillLog = value;
        }
    }
    
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

    protected virtual async void EffectImpact(long impactTime)
    {
        try
        {
            if (Room == null) return; // Effect는 Target이 없는 경우도 있음
            await Scheduler.ScheduleEvent(impactTime, () =>
            {
                if (Room == null) return;
                if (Parent is Creature creature) creature.ApplyEffectEffect();
                Room?.Push(Room.LeaveGameOnlyServer, Id);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}