using Google.Protobuf.Protocol;

namespace Server.Game;

public class Bunny : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BunnyHealth:
                    MaxHp += 20;
                    Hp += 20;
                    BroadcastHp();
                    break;
                case Skill.BunnyEvasion:
                    Evasion += 5;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile3, this, 5f);
        });
    }
}