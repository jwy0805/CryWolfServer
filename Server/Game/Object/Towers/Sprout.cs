using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Sprout : Seed
{
    private bool _drain = false;
    private bool _fire = false;
    
    protected readonly float DrainParam = DataManager.SkillDict[(int)Skill.SproutDrain].Value;
    
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
                    FireResist += (int)DataManager.SkillDict[(int)Skill].Value;
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
            Room.SpawnProjectile(_fire ? ProjectileId.SproutFire : ProjectileId.SeedProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (pid == ProjectileId.SproutFire)
        {
            Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);

        // Drain
        if (_drain == false) return;
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
    }

    public virtual void ApplyProjectileEffect2(GameObject target, ProjectileId pid) { }
}