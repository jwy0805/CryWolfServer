using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Shell : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.ShellDefence:
                    Defence += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.ShellFireResist:
                    FireResist += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.ShellPoisonResist:
                    PoisonResist += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
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