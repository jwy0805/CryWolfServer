namespace Server.Game;

public class SnowBombSkill : Projectile
{
    protected override async void AttackImpactTime(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Parent is SnowBomb snowBomb) snowBomb.ApplyProjectileEffect(Target, ProjectileId, Target.PosInfo);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}