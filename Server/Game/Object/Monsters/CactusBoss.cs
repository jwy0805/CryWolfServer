using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;
using Random = System.Random;

namespace Server.Game;

public class CactusBoss : Cactus
{
    private bool _rush = false;
    private bool _breath = false;
    private bool _breathHeal = false;
    private bool _breathAggro = false;
    private bool _rushed = false;
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
        // Player.SkillSubject.SkillUpgraded(Skill.CactusBossRush);
        // Player.SkillSubject.SkillUpgraded(Skill.CactusBossBreath);
        // Player.SkillSubject.SkillUpgraded(Skill.CactusBossHeal);
        // Player.SkillSubject.SkillUpgraded(Skill.CactusBossAggro);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
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
                UpdateAttack3();
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
        {   // Attack3 = SMASH Animation
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
    
    protected override void UpdateAttack3()
    {
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long impactMomentCorrection = LastAnimEndTime - timeNow + impactMoment;
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        SmashImpactEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
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
            Target.OnDamaged(this, SmashDamage, Damage.Normal);
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
        var types = new List<GameObjectType> { GameObjectType.Tower, GameObjectType.Sheep };
        var targetList = Room.FindTargetsInAngleRange(this, types, 80, 90);
        foreach (var target in targetList)
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            if (_breathAggro) BuffManager.Instance.AddBuff(BuffId.Aggro, target, this, 0, 2000);
        }
        if (_breathHeal) BuffManager.Instance.AddBuff(BuffId.Heal, 
            this, this, HealParam * targetList.Count, 1000, true);
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

        State =  Mp >= MaxMp && _breath ? State.Skill : GetRandomState(State.Attack, State.Attack2);
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
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker.OnDamaged(this, reflectionDamage, damageType, true);
            if (new Random().Next(99) < ReflectionFaintRate && attacker.Targetable)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, attacker, this, 0, 1000);
            }
        }
    }
}