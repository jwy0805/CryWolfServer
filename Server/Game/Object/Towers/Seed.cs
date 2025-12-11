using Google.Protobuf.Protocol;
using Server.Data;

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
                    Evasion += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.SeedRange:
                    AttackRange += DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });
    }
}