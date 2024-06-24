using Google.Protobuf.Protocol;

namespace Server.Game;

public class Seed : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SeedEvasion:
                    Evasion += 10;
                    break;
                case Skill.SeedRange:
                    AttackRange += 1;
                    break;
            }
        }
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId _)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
}