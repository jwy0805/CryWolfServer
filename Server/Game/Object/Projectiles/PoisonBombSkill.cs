namespace Server.Game;

public class PoisonBombSkill : Projectile
{
    protected override async void AttackImpact(long impactTime)
    {
        if (Target == null || Target.Targetable == false || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null) return;
            if (Parent is SnowBomb snowBomb) snowBomb.ApplyProjectileEffect(Target, ProjectileId, Target.PosInfo);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}