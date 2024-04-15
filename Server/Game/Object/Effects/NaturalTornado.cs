using Google.Protobuf.Protocol;

namespace Server.Game;

public class NaturalTornado : Effect
{
    private long _damageTime = 0;
    
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
        
        if (Room.Stopwatch.ElapsedMilliseconds > _damageTime + 1000)
        {
            SetEffectEffect();
            if (Room != null) _damageTime = Room.Stopwatch.ElapsedMilliseconds;
        }
    }

    protected override void SetEffectEffect()
    {
        if (Target == null || Target.Targetable == false)
        {
            Room?.LeaveGame(Id);
            return;
        }
        int damage = Target.MaxHp / 10;
        Target.OnDamaged(this, damage, Damage.Magical);
    }
}