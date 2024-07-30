using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Soul : Tower
{
    private bool _drain = false;
    protected readonly float DrainParam = 0.2f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SoulAttack:
                    Attack += 10;
                    break;
                case Skill.SoulAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.1f;
                    break;
                case Skill.SoulDrain:
                    _drain = true;
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
            if (State == State.Faint) return;            
            Room.SpawnProjectile(ProjectileId.SoulProjectile, this, 5f);
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null) return;
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        
        // Drain
        if (_drain == false) return;
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
    }
}