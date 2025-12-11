using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
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
                    AttackSpeed += AttackSpeed * (DataManager.SkillDict[(int)Skill].Value / (float)100);
                    break;
                case Skill.BudRange:
                    AttackRange += DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.BudAccuracy:
                    Accuracy += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
        AttackImpactMoment = 0.3f;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }
}