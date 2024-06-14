using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class PoisonBomb : SnowBomb
{
    private bool _selfDestruct;
    private bool _mpDown;
    private readonly int _increasingMaxMpParam = 20;
    private int _poisonParam = 3;
    
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
                    _poisonParam = 6;
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
        SelfExplosionRange = 3.5f;
        Player.SkillUpgradedList.Add(Skill.PoisonBombSelfDestruct);
    }

    protected override void UpdateRush()
    {
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
        Room.SpawnEffect(EffectId.PoisonBombExplosion, this, PosInfo);
        
        if (_selfDestruct)
        {
            var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
            var gameObjects = Room.FindTargets(this, targetList, SelfExplosionRange);

            foreach (var gameObject in gameObjects)
            {
                gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
                BuffManager.Instance.AddBuff(BuffId.Addicted, gameObject, this, _poisonParam, 5000);
                if (_mpDown == false) continue;
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
                BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, gameObject, this, 3, 5000);
            }
        }
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid, PositionInfo posInfo)
    {
        if (Room == null || Hp <= 0) return;
        Room.SpawnEffect(EffectId.PoisonBombSkillExplosion, this, posInfo);
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var cellPos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
        var gameObjects = Room.FindTargets(cellPos, targetList, ExplosionRange);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            BuffManager.Instance.AddBuff(
                BuffId.AttackSpeedDecrease, gameObject, this, AttackDecreaseParam, 5000);
        }
    }

    public override void OnDamaged(GameObject? attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (new Random().Next(100) < TotalEvasion)
        {
            // TODO: Evasion Effect
            return;
        }

        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker?.OnDamaged(this, reflectionDamage, damageType, true);
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp > 0) return;
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
}