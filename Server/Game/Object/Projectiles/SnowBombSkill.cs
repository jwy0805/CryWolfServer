namespace Server.Game;

public class SnowBombSkill : Projectile
{
    protected override async void AttackImpactTime(long impactTime)
    {
        if (Target == null || Target.Targetable == false || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null) return;
            if (Parent is Bomb bomb) bomb.ApplyProjectileEffect(Target, ProjectileId, Target.PosInfo);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}