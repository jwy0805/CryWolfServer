using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBomb : SnowBomb
{
    private bool _selfDestruct;
    private bool _mpDown;
    private readonly int _increasingMaxMpParam = 20;
    private float _poisonParam = 0.03f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PoisonBombBombRange:
                    ExplosionRange = 2.0f;
                    break;
                case Skill.PoisonBombSelfDestruct:
                    _selfDestruct = true;
                    break;
                case Skill.PoisonBombPoisonPowerUp:
                    _poisonParam = 0.06f;
                    break;
                case Skill.PoisonBombExplosionMpDown:
                    _mpDown = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
        SelfExplosionRange = 3.5f;
        Player.SkillUpgradedList.Add(Skill.PoisonBombSelfDestruct);
    }

    protected override void UpdateRush()
    {
        if (Room == null) return;
        
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        if (distance <= 0.8f)
        {
            Room.Map.ApplyMap(this, CellPos, false);
            State = State.Explode;
            ExplodeEvents((long)(StdAnimTime * SkillImpactMoment2));
            return;
        }
        
        Path = Room.Map.MoveIgnoreCollision(this);
        BroadcastPath();
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            ApplyAttackEffect(Target);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.PoisonBombSkill, this, 5f);
        });
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null || AddBuffAction == null) return;
        Room.SpawnEffect(EffectId.PoisonBombExplosion, this, PosInfo);
        
        if (_selfDestruct)
        {
            var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
            var gameObjects = Room.FindTargets(this, targetList, SelfExplosionRange);

            foreach (var gameObject in gameObjects)
            {
                Room.Push(gameObject.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
                Room.Push(AddBuffAction, BuffId.Addicted,
                    BuffParamType.Percentage, gameObject, this, _poisonParam, 5000, false);
                if (_mpDown == false || gameObject.Room == null) continue;
                gameObject.MaxMp *= _increasingMaxMpParam;
                gameObject.BroadcastMp();
            }
        }
        else
        {
            var targetList = new[] { GameObjectType.Monster };
            var gameObjects = Room.FindTargets(this, targetList, SkillRange);

            foreach (var gameObject in gameObjects)
            {
                Room.Push(AddBuffAction, BuffId.Addicted,
                    BuffParamType.Constant, gameObject, this, 3, 5000, false);
            }
        }
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid, PositionInfo posInfo)
    {
        if (Room == null || Hp <= 0 || AddBuffAction == null) return;
        Room.SpawnEffect(EffectId.PoisonBombSkillExplosion, this, posInfo);
        
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var cellPos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
        var gameObjects = Room.FindTargets(cellPos, targetList, ExplosionRange);
        
        foreach (var gameObject in gameObjects)
        {
            Room.Push(gameObject.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            Room.Push(AddBuffAction, BuffId.AttackSpeedDebuff,
                BuffParamType.Constant, gameObject, this, AttackSpeedDecreaseParam, 5000, false);
        }
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        var random = new Random();
        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        
        if (random.Next(100) < attacker.CriticalChance)
        {
            totalDamage = (int)(totalDamage * attacker.CriticalMultiplier);
        }
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {
            // TODO: Evasion Effect
            return;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);

        if (Hp <= 0)
        {
            AttackEnded = true;
            IsAttacking = false;
            Targetable = false;
            Attacker = attacker;
            if (_selfDestruct)
            {
                var target = Room.FindRandomTarget(
                    this, TotalAttackRange * 3, 0, true);
                DestPos = target == null 
                    ? new Vector3(CellPos.X, CellPos.Y, CellPos.Z) 
                    : new Vector3(target.CellPos.X, target.CellPos.Y, target.CellPos.Z);
                MoveSpeed = 16;
                State = State.Rush;
            }
            else
            {
                State = State.Explode;
                ExplodeEvents((long)(StdAnimTime * SkillImpactMoment2));
            }
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
        }
    }
}