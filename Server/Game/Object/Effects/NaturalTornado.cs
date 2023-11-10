namespace Server.Game;

public class NaturalTornado : Effect
{
    private long _damageTime = 0;
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Parent == null) Room?.LeaveGame(Id);
        else CellPos = Parent.CellPos;
        
        if (Room?.Stopwatch.ElapsedMilliseconds > _damageTime + 1000)
        {
            SetEffectEffect();
            _damageTime = Room.Stopwatch.ElapsedMilliseconds;
        }
    }

    protected override void SetEffectEffect()
    {
        if (Parent == null || Room == null) return; 
        int damage = Parent.MaxHp / 10;
        Parent.OnDamaged(this, damage);
    }
}