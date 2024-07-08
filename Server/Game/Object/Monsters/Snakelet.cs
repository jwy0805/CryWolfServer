using Google.Protobuf.Protocol;

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
                    AttackSpeed += AttackSpeed * 0.2f;
                    break;
                case Skill.SnakeletAttack:
                    Attack += 6;
                    break;
                case Skill.SnakeletEvasion:
                    Evasion += 15;
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
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile2, this, 5f);
        });
    }
}