namespace Server.Game;

public class PurpleBeam : Effect
{
    private long _damageTime = 0;
    private readonly long _duration = 3000;
    private readonly long _startTime = 600;
    private readonly long _interval = 300;
    private readonly int _skillDamage = 50;

    public override void Init()
    {
        if (Room == null) return;
        _damageTime = Room.Stopwatch.ElapsedMilliseconds + _startTime;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Target == null)
        {
            Room.LeaveGame(Id);
            return;
        }
        
        CellPos = Target.CellPos;
        
        if (Room.Stopwatch.ElapsedMilliseconds > _damageTime + _interval)
        {
            SetEffectEffect();
            _damageTime = Room.Stopwatch.ElapsedMilliseconds;
        }
    }

    protected override void SetEffectEffect()
    {
        if (Room == null) return;
        if (Room.Stopwatch.ElapsedMilliseconds > _damageTime + _duration - _startTime || Target == null)
        {
            Room.LeaveGame(Id);
            return;
        }
        Target.OnDamaged(this, _skillDamage);
    }
}