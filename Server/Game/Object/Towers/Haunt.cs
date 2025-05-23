using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Haunt : Soul
{
    private bool _fire = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HauntFireResist:
                    FireResist += 10;
                    break;
                case Skill.HauntPoisonResist:
                    PoisonResist += 10;
                    break;
                case Skill.HauntFire:
                    _fire = true;
                    break;
                case Skill.HauntRange:
                    AttackRange += 2.0f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            Room.SpawnProjectile(_fire ? ProjectileId.HauntFire : ProjectileId.HauntProjectile, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (pid == ProjectileId.HauntFire)
        {
            Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);

        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
    }
}