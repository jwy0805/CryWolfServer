namespace Server.Game;

public class SproutFire : Projectile
{
    public short Depth { get; set; }

    protected override async Task AttackImpact(long impactTime)
    {
        if (Parent == null || Target == null || Target.Targetable == false || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Parent == null || Target == null || Target.Targetable == false || Room == null) return;
            if (Parent is Sprout sprout)
            {
                if (Depth == 0) sprout.ApplyProjectileEffect(Target, ProjectileId);
                else sprout.ApplyProjectileEffect2(Target, ProjectileId);
            }
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}