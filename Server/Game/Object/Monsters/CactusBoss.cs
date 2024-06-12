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
    private bool _start = false;
    private bool _speedRestore = false;
    private bool _firstAttack = false;
    private readonly int _rushSpeed = 4;
    private int HealParam => 100 + SkillParam;
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
        Player.SkillUpgradedList.Add(Skill.CactusBossRush);
        Player.SkillUpgradedList.Add(Skill.CactusBossBreath);
        Player.SkillUpgradedList.Add(Skill.CactusBossHeal);
        Player.SkillUpgradedList.Add(Skill.CactusBossAggro);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 20;
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
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        if (_rush && _start == false)
        {
            _start = true;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
    }

    protected override void UpdateMoving()
    {
        if (_rush && _start == false) // rush 업그레이드, 처음 spawn 되었을 때
        {
            _start = true;
            MoveSpeedParam += _rushSpeed;
            State = State.Rush;
            return;
        }
        
        if (_rush && _start && _speedRestore == false) // rush 이후 다시 moving일 때
        {
            MoveSpeedParam -= _rushSpeed;
            _speedRestore = true;
        }
        
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
        // Target이 사정거리 안에 있다가 밖으로 나간 경우 애니메이션 시간 고려하여 Attack 상태로 변경되도록 조정
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        if (distance <= TotalAttackRange)
        {
            if (LastAnimEndTime != 0 && timeNow <= LastAnimEndTime + animPlayTime) return;
            State = Mp >= MaxMp ? State.Skill : State.Attack;
            SetDirection();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void UpdateRush()
    {
        Target = Room.FindClosestTarget(this);
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
        {   // Attack3 = SMASH Animation
            State = State.Attack3;
            SetDirection();
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
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
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
    
    protected override void UpdateSkill()
    {
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long impactMomentCorrection = LastAnimEndTime - timeNow + impactMoment;
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        SkillImpactEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }

    private void SmashImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnEffect(EffectId.CactusBossSmashEffect, Target, Target.PosInfo);
            ApplySmashEffect(Target);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnEffect(EffectId.CactusBossBreathEffect, this);
            ApplyBreathEffect(Target);
            Mp = 0;
        });
    }

    private void ApplySmashEffect(GameObject? target)
    {
        target?.OnDamaged(this, SmashDamage, Damage.Normal);
    }

    private void ApplyBreathEffect(GameObject? target)
    {
        var types = new HashSet<GameObjectType> { GameObjectType.Tower, GameObjectType.Sheep };
        var targetList = Room.FindTargetsInAngleRange(this, types, 80, 45);
        Console.WriteLine(targetList.Count);
        foreach (var gameObject in targetList)
        {
            target?.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            if (_breathHeal) Hp += HealParam;
            if (_breathAggro) BuffManager.Instance.AddBuff(BuffId.Aggro, gameObject, this, 0, 2000);
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

        State =  Mp >= MaxMp ? State.Skill : GetRandomState(State.Attack, State.Attack2);
        SetDirection();
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;

        int totalDamage;
        if (damageType is Damage.Normal or Damage.Magical)
        {
            totalDamage = attacker.CriticalChance > 0 
                ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                : Math.Max(damage - TotalDefence, 0);
            if (damageType is Damage.Normal && Reflection && reflected == false)
            {
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
                var random = new Random();
                if (random.Next(99) >= ReflectionFaintRate) return;
                BuffManager.Instance.AddBuff(BuffId.Fainted, attacker, this, 0, 1000);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
    }
    
    public override void ApplyAdditionalAttackEffect(GameObject target)
    {
       if (_firstAttack == false)
       {
           _firstAttack = true;
           target.OnDamaged(this, SmashDamage, Damage.Normal);
       }
       else
       {
           target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
           if (_breathHeal)
           {
               Hp += HealParam;
               Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
           }
           if (_breathAggro)
           {
               var towers = Room.FindTargets(
                   this, new List<GameObjectType> { GameObjectType.Tower }, SkillRange);
               foreach (var tower in towers)
               {
                   BuffManager.Instance.AddBuff(BuffId.Aggro, tower, this, 0, 2000);
               } 
           }
       }

       Mp += 5;
    }
}