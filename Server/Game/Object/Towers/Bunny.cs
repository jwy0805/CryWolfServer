using Google.Protobuf.Protocol;
using Server.Data;

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
                    MaxHp += (int)DataManager.SkillDict[(int)Skill].Value;
                    Hp += (int)DataManager.SkillDict[(int)Skill].Value;
                    BroadcastHp();
                    break;
                case Skill.BunnyEvasion:
                    Evasion += (int)DataManager.SkillDict[(int)Skill].Value;
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