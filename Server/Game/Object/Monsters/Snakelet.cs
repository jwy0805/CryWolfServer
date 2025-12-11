using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Snakelet : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeletAttackSpeed:
                    AttackSpeed += AttackSpeed * DataManager.SkillDict[(int)Skill].Value / 100f;
                    break;
                case Skill.SnakeletAttack:
                    Attack += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.SnakeletEvasion:
                    Evasion += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
        AttackImpactMoment = 0.25f;
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile2, this, 5f);
        });
    }
}