using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Bud : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BudAttackSpeed:
                    break;
                case Skill.BudRange:
                    break;
                case Skill.BudAccuracy:
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        AttackImpactMoment = 0.3f;
    }
    
    protected override async void AttackImpactEvents(long impactTime)
    {
        if (Target == null || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
}