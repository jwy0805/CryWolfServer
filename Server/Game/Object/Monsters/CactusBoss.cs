using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;
using Random = System.Random;

namespace Server.Game;

public class CactusBoss : Cactus
{
    private bool _rush;
    private bool _breath;
    private bool _breathHeal;
    private bool _breathAggro;
    private bool _rushed;
    private readonly int _rushSpeed = 3;
    private int HealParam => 60 + SkillParam;
    private int SmashDamage => 150 + SkillParam;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactusBossRush:
                    _rush = true;
                    break;
                case Skill.CactusBossBreath:
                    _breath = true;
                    break;
                case Skill.CactusBossHeal:
                    _breathHeal = true;
                    break;
                case Skill.CactusBossAggro:
                    _breathAggro = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
        ReflectionRate = 10;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime  && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }
        
        switch (State)
        {
            case State.Die:
                UpdateDie();
                break;
            case State.Moving:
                UpdateMoving();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Rush:
                UpdateRush();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Attack2:
                UpdateAttack2();
                break;
            case State.Attack3:
                UpdateAttack3();    // 달려간 후 Smash
                break;
            case State.Skill:
                UpdateSkill();
                break;
            case State.Skill2:
                UpdateSkill2();
                break;
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }   
    }
    
    protected override void UpdateIdle()
    {
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        if (_rush && _rushed == false)
        {
            MoveSpeed += _rushSpeed;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
    }

    protected override void UpdateMoving()
    {
        // Targeting
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
            State = Mp >= MaxMp && _breath ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void UpdateRush()
    {
        if (Room == null) return;
        
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            MoveSpeed -= _rushSpeed;
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
            // Attack3 = SMASH Animation
            _rushed = true;
            MoveSpeed -= _rushSpeed;            
            State = State.Attack3;
            SyncPosAndDir();
            return;
        }
        
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void UpdateAttack2()
    {
        UpdateAttack();
    }
    
    protected override void UpdateAttack3() { }

    protected override void OnAttack3()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Hp <= 0) return;
        
        Room.Broadcast(new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        });
        var impactMoment = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactMoment);
        var animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        SmashImpactEvents(impactMoment);
        EndEvents(animPlayTime); 
    }

    private void SmashImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            var posInfo = new PositionInfo
            {
                Dir = Dir, PosX = Target.PosInfo.PosX, PosY = Target.PosInfo.PosY, PosZ = Target.PosInfo.PosZ
            };
            Room.SpawnEffect(EffectId.CactusBossSmashEffect, Target, posInfo);
            Room.Push(Target.OnDamaged, this, SmashDamage, Damage.Normal, false);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnEffect(EffectId.CactusBossBreathEffect, this);
            ApplyBreathEffect();
            Mp = 0;
        });
    }

    private void ApplyBreathEffect()
    {
        if (Room == null || AddBuffAction == null) return;
        
        var types = new List<GameObjectType> { GameObjectType.Tower, GameObjectType.Sheep };
        var targetList = Room.FindTargetsInAngleRange(this, types, 80, 90);
        foreach (var target in targetList)
        {
            Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            
            // Breath Aggro
            if (_breathAggro)
            {
                Room.Push(AddBuffAction, BuffId.Aggro,
                    BuffParamType.None, target, this, 0, 2000, false);
            }
        }
        
        // Breath Heal
        if (_breathHeal)
        {
            Room.Push(AddBuffAction, BuffId.HealBuff, 
                BuffParamType.Constant, this, this, HealParam * targetList.Count, 1000, true);
        }
    }

    protected override void SetNextState()
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

        State =  Mp >= MaxMp && _breath ? State.Skill : GetRandomState(State.Attack, State.Attack2);
        SyncPosAndDir();
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null || AddBuffAction == null) return;
        if (Invincible) return;
        
        var random = new Random();
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
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

        totalDamage = damageType is Damage.Normal or Damage.Magical
            ? Math.Max(totalDamage - TotalDefence, 0) : damage;
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
            if (random.Next(99) < ReflectionFaintRate && attacker.Targetable)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, attacker, this, 0, 2000, false);   
            }
        }
    }
}