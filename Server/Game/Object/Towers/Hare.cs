using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hare : Rabbit
{
    private bool _punch = false;
    private bool _defenceDown = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HarePunch:
                    _punch = true;
                    break;
                case Skill.HarePunchDefenceDown:
                    _defenceDown = true;
                    break;
                case Skill.HareEvasion:
                    Evasion += 12;
                    break;
            }
        }
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            
            Room.SpawnProjectile(ProjectileId.HarePunch, this, 5f);
            Mp = 0;
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0) return;
        if (pid == ProjectileId.HarePunch)
        {
            if (target is not Creature creature) return;
            BuffManager.Instance.AddBuff(BuffId.Aggro, creature, this, 0, 2000);
        }
        else
        {
            target.OnDamaged(this, TotalAttack, Damage.Normal);
        }
    }
}