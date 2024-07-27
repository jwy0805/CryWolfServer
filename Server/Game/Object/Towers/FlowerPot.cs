using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class FlowerPot : Sprout
{
    private bool _3Hit;
    private bool _fireResistDown;
    private bool _doubleTarget;
    private bool _lostHealthAttack;
    private float _lostHealAttackParam = 1;
    private short _hitCount;
    private Projectile _projectile = new();
    private Projectile? _projectile2;
    private readonly float _doubleTargetParam = 0.6f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.FlowerPot3Hit:
                    _3Hit = true;
                    break;
                case Skill.FlowerPotFireResistDown:
                    _fireResistDown = true;
                    break;
                case Skill.FlowerPotDoubleTargets:
                    _doubleTarget = true;
                    break;
                case Skill.FlowerPotLostHealthAttack:
                    _lostHealthAttack = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
        Player.SkillSubject.SkillUpgraded(Skill.FlowerPot3Hit);
        Player.SkillSubject.SkillUpgraded(Skill.FlowerPotFireResistDown);
        Player.SkillSubject.SkillUpgraded(Skill.FlowerPotDoubleTargets);
        Player.SkillSubject.SkillUpgraded(Skill.FlowerPotLostHealthAttack);
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            
            _hitCount++;
            if (_hitCount == 3 && _3Hit)
            {
                _hitCount = 0;
                _projectile = Room.SpawnProjectile(ProjectileId.Sprout3HitFire, this, 5f);
            }
            else
            {
                _projectile = Room.SpawnProjectile(ProjectileId.SproutFire, this, 5f);
            }
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        if (_fireResistDown) target.FireResistParam -= 2;
        if (_lostHealthAttack) _lostHealAttackParam = 1 + (1 - target.Hp / (float)target.MaxHp) * 0.5f;
        
        Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
        var drain = Math.Max((int)(TotalAttack * _lostHealAttackParam) - target.TotalDefence, 0);
        Room.Push(target.OnDamaged, this, (int)(TotalAttack * _lostHealAttackParam), Damage.Normal, false);
        Hp += (int)(drain * DrainParam);
        
        if (pid == ProjectileId.Sprout3HitFire)
        {
            var additionalDamage = Math.Max((int)(TotalAttack * _lostHealAttackParam) - target.TotalDefence, 0);
            Room.Push(target.OnDamaged, this, additionalDamage, Damage.Magical, false);
            Hp += (int)(additionalDamage * DrainParam);
        }

        _projectile.CellPos = target.CellPos;
        
        if (_doubleTarget)
        {
            var types = new[]{ GameObjectType.Monster, GameObjectType.MonsterStatue };
            var secondTarget = Room.FindTargets(_projectile.CellPos, types, TotalAttackRange, AttackType)
                .Where(gameObject => gameObject.Id != target.Id)
                .MinBy(gameObject => Vector3.Distance(
                    _projectile.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 }));
            if (secondTarget == null) return;
            
            _projectile2 = Room.SpawnProjectile(
                pid == ProjectileId.Sprout3HitFire ? ProjectileId.Sprout3HitFire : ProjectileId.SproutFire,
                this, target.PosInfo, 5f, secondTarget);
            if (_projectile2 is SproutFire projectile2) projectile2.Depth = 1;
        }
    }

    public override void ApplyProjectileEffect2(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        if (_lostHealthAttack) _lostHealAttackParam = 1 + (1 - target.Hp / (float)target.MaxHp) * 0.5f;
        if (_fireResistDown) target.FireResistParam -= 1;
        
        Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);        
        var damage = (int)(TotalAttack * _doubleTargetParam * _lostHealAttackParam);
        var drain = Math.Max(damage - target.TotalDefence, 0);
        Room.Push(target.OnDamaged, this, damage, Damage.Normal, false);
        Hp += (int)(drain * DrainParam);

        if (pid != ProjectileId.Sprout3HitFire) return;
        var additionalDamage = Math.Max(
            (int)(TotalAttack * _doubleTargetParam * _lostHealAttackParam) - target.TotalDefence, 0);
        Room.Push(target.OnDamaged, this, additionalDamage, Damage.Magical, false);
        Hp += (int)(additionalDamage * DrainParam);
    }
}