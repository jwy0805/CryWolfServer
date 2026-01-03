using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;

namespace Server.Game;

public class PoisonBomb : SnowBomb
{
    private bool _magicalAttackBuff;
    private bool _recoverPoison;
    private bool _poisonBomb;
    
    private readonly float _poisonParam = 0.02f;
    
    private readonly int _magicalAttackBuffParam = (int)DataManager.SkillDict[(int)Skill.PoisonBombMagicalAttack].Value;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PoisonBombMagicalAttack:
                    _magicalAttackBuff = true;
                    break;
                case Skill.PoisonBombRecoverPoison:
                    _recoverPoison = true;
                    break;
                case Skill.PoisonBombPoison:
                    _poisonBomb = true;
                    break;
                case Skill.PoisonBombBombRange:
                    ExplosionRange += 1f;
                    SelfExplosionRange += 1f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
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
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            ApplyAttackEffect(Target);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            Target = Room
                .FindTargets(this, new[] { GameObjectType.Monster }, TotalAttackRange, 2)
                .OrderBy(go => go.CellPos.Z)
                .FirstOrDefault(go => go.Id != Id);
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.PoisonBombSkill, this, 5f);
        });
    }
    
    // When Self Destruct
    public override void ApplyEffectEffect()
    {
        if (Room == null || AddBuffAction == null) return;
        Room.SpawnEffect(EffectId.PoisonBombExplosion, this, this, PosInfo);
        var allyList = new[] { GameObjectType.Monster };
        var allies = Room.FindTargets(this, allyList, SkillRange);
        foreach (var ally in allies)
        {
            Room.Push(AddBuffAction, BuffId.MagicalDefenceBuff,
                BuffParamType.Constant, ally, this, MagicalDefenceBuffParam, 5000, false);

            if (!_recoverPoison) continue;
            if (ally is not Creature creature) continue;
            Room.Push(Room.RemoveNestedBuff, BuffId.Addicted, creature);
        }

        if (_poisonBomb)
        {
            var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
            var enemies = Room.FindTargets(this, targetList, SelfExplosionRange);
            foreach (var enemy in enemies)
            {
                Room.Push(AddBuffAction, BuffId.Addicted,
                    BuffParamType.Percentage, enemy, this, _poisonParam, 5000, false);
            }
        }
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid, PositionInfo posInfo)
    {
        if (Room == null || Hp <= 0 || AddBuffAction == null) return;
        Room.SpawnEffect(EffectId.PoisonBombSkillExplosion, this, this, posInfo);
        var enemyList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var cellPos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
        var enemies = Room.FindTargets(cellPos, enemyList, ExplosionRange);
        foreach (var enemy in enemies)
        {
            Room.Push(enemy.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            Room.Push(AddBuffAction, BuffId.AttackSpeedDebuff,
                BuffParamType.Percentage, enemy, this, AttackSpeedDecreaseParam, 5000, false);
        }
        
        var allies = Room.FindTargets(cellPos, new[] { GameObjectType.Monster }, ExplosionRange);
        foreach (var ally in allies)
        {
            ally.AttackParam += AttackBuffParam;
            if (_magicalAttackBuff)
            {
                ally.SkillParam += _magicalAttackBuffParam;
            }
        }
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        
        var random = new Random();
        S_GetDamage damagePacket;
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {  
            // Evasion
            damagePacket = new S_GetDamage { ObjectId = Id, DamageType = Damage.Miss, Damage = 0 };
            Room.Broadcast(damagePacket);
            return;
        }
        
        // 일반적으로 Normal Damage 만 Critical 가능, Magical이나 True Damage Critical 구현 시 데미지를 넣는 Unit으로부터 자체적으로 계산
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;
        
        if (ShieldRemain > 0)
        {   
            // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain < 0)
            {
                totalDamage = Math.Abs(ShieldRemain);
                ShieldRemain = 0;
            }
        }

        totalDamage = GameManager.Instance.CalcDamage(this, damageType, totalDamage);
        Hp = Math.Max(Hp - totalDamage, 0);
        damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);

        if (Hp <= 0)
        {
            AttackEnded = true;
            Targetable = false;
            Attacker = attacker;
            if (_recoverPoison)
            {
                var target = Room.FindRandomTarget(
                    this, TotalAttackRange * 3, 0, true);
                if (target != null)
                {
                    CellPos = new Vector3(target.CellPos.X, target.CellPos.Y, target.CellPos.Z);
                    BroadcastInstantMove();
                }
                State = State.Explode;
                ExplodeEvents((long)(StdAnimTime * SkillImpactMoment2));
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