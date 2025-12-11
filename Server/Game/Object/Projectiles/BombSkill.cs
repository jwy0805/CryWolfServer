namespace Server.Game;

public class BombSkill : Projectile
{
    protected override async Task AttackImpact(long impactTime)
    {
        if (Parent == null || Target == null || Target.Targetable == false || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Parent == null || Target == null || Target.Targetable == false || Room == null) return;
            if (Parent is Bomb bomb) bomb.ApplyProjectileEffect(Target, ProjectileId, Target.PosInfo);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}