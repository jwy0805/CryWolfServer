using Google.Protobuf.Protocol;

namespace Server.Game;

public class Sprout : Tower
{
    private bool _drain = false;
    private bool _fire = false;
    
    protected readonly float DrainParam = 0.15f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SproutDrain:
                    _drain = true;
                    break;
                case Skill.SproutFireAttack:
                    _fire = true;
                    break;
                case Skill.SproutFireResist:
                    FireResist += 10;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        Player.SkillSubject.SkillUpgraded(Skill.SproutDrain);
        Player.SkillSubject.SkillUpgraded(Skill.SproutFireAttack);
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(_fire ? ProjectileId.SproutFire : ProjectileId.SeedProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (pid == ProjectileId.SproutFire)
        {
            BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 0, 5000);
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);

        if (_drain == false) return;
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
    }
}