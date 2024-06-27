using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBomb : Bomb
{
    private bool _areaAttack;
    private bool _frostbite;
    private bool _frostArmor;

    protected float ExplosionRange = 1.5f;
    protected float SelfExplosionRange = 2.5f;
    protected readonly float AttackSpeedDecreaseParam = 0.1f;
    protected GameObject? Attacker;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnowBombFireResist:
                    FireResist += 20;
                    break;
                case Skill.SnowBombAreaAttack:
                    _areaAttack = true;
                    break;
                case Skill.SnowBombFrostbite:
                    _frostbite = true;
                    break;
                case Skill.SnowBombFrostArmor:
                    _frostArmor = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
        SkillImpactMoment2 = 1.0f;
        Player.SkillUpgradedList.Add(Skill.SnowBombAreaAttack);
        Player.SkillUpgradedList.Add(Skill.SnowBombFrostArmor);
    }

    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance <= TotalAttackRange)
        {
            State = Mp >= MaxMp ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SnowBombSkill, this, 5f);
            Mp = 0;
        });
    }

    protected async void ExplodeEvents(long impactTime)
    {
        if (Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            ApplyEffectEffect();
            OnDead(Attacker);
        });
    }

    public override void ApplyEffectEffect()
    {
        Room?.SpawnEffect(EffectId.SnowBombExplosion, this, PosInfo);
        var targetList = new[] { GameObjectType.Monster };
        var gameObjects = Room.FindTargets(this, targetList, SkillRange);
        foreach (var gameObject in gameObjects)
        {
            BuffManager.Instance.AddBuff(BuffId.DefenceDebuff, BuffParamType.Constant, 
                gameObject, this, 3, 5000);
        }
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid, PositionInfo posInfo)
    {
        if (Room == null || Hp <= 0) return;
        if (pid == ProjectileId.BombProjectile)
        {
            target?.OnDamaged(this, TotalAttack, Damage.Normal);
        }
        else
        {
            if (_areaAttack)
            {
                Room.SpawnEffect(EffectId.SnowBombExplosion, this, posInfo);
                var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
                var cellPos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
                var gameObjects = Room.FindTargets(cellPos, targetList, ExplosionRange);
                foreach (var gameObject in gameObjects)
                {
                    gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
                    if (_frostbite == false) continue;
                    BuffManager.Instance.AddBuff(BuffId.AttackSpeedDebuff, BuffParamType.Percentage,
                        gameObject, this, AttackSpeedDecreaseParam, 5000);
                }
            }
            else
            {
                target?.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            }
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        State =  Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
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
            if (_frostArmor)
            {
                Targetable = false;
                State = State.Explode;
                Attacker = attacker;
                ExplodeEvents((long)(StdAnimTime * SkillImpactMoment2));
            }
            else
            {
                OnDead(attacker);
            }
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker.OnDamaged(this, reflectionDamage, damageType, true);
        }
    }
}