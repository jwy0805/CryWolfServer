using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Creature : GameObject
{
    protected virtual Skill NewSkill { get; set; }
    protected Skill Skill;
    protected readonly Scheduler Scheduler = new();
    protected readonly List<Skill> SkillList = new();
    protected bool StateChanged;
    protected bool IsAttacking;
    protected bool AttackEnded = true;
    protected long LastAnimEndTime;
    protected float AttackImpactMoment = 0.5f;
    protected float SkillImpactMoment = 0.5f;
    protected float SkillImpactMoment2 = 0.5f;
    protected Guid AttackTaskId;
    protected Guid EndTaskId;
    protected const long MpTime = 1000;
    protected const long StdAnimTime = 1000;
    
    public UnitId UnitId { get; set; }
    public Role UnitRole { get; set; }
    public virtual bool Degeneration { get; set; }

    public override State State
    {
        get => PosInfo.State;
        set
        {
            var preState = PosInfo.State;
            PosInfo.State = value;
            if (preState != PosInfo.State) DistRemainder = 0;
            BroadcastState();
        }
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
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

    protected virtual void UpdateIdle() { }

    protected virtual void UpdateMoving()
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
            State = State.Attack;
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected virtual void UpdateAttack()
    {
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;

        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
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
        AttackImpactEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }   
    
    protected virtual void UpdateAttack2() { }
    protected virtual void UpdateAttack3() { }

    protected virtual void UpdateSkill()
    {
        // 첫 UpdateSkill Cycle시 아래 코드 실행
        if (IsAttacking) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
            return;
        }
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
    
    protected virtual void UpdateSkill2() { }
    protected virtual void UpdateKnockBack() { }
    protected virtual void UpdateRush() { }
    protected virtual void UpdateDie() { }
    public virtual void RunSkill() { }

    public virtual void OnFaint()
    {
        State = State.Faint;
        IsAttacking = false;
        AttackEnded = true;
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
    }

    
    protected virtual void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            ApplyAttackEffect(Target);
        });
    }

    protected virtual void SkillImpactEvents(long impactTime) { }

    protected virtual void MotionChangeEvents(long time) { }

    protected virtual void EndEvents(long animEndTime)
    {
        EndTaskId = Scheduler.ScheduleCancellableEvent(animEndTime, () =>
        {
            if (Room == null || Hp <= 0) return;
            SetNextState();
            LastAnimEndTime = Room.Stopwatch.ElapsedMilliseconds;
            IsAttacking = false;
        });
    }
    
    protected virtual async void DieEvents(long standbyTime)
    {
        await Scheduler.ScheduleEvent(standbyTime, () =>
        {
            if (Room == null) return;
            State = State.Revive;
            ReviveEvents(StdAnimTime);
        });
    }

    protected virtual async void ReviveEvents(long reviveAnimTime)
    {
        await Scheduler.ScheduleEvent(reviveAnimTime, () =>
        {
            if (Room == null) return;
            Hp += (int)(MaxHp * ReviveHpRate);
            BroadcastHp();
            WillRevive = false;
            AlreadyRevived = true;
            Targetable = true;
            State = State.Idle;
        });
    }

    
    public virtual void ApplyAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    public virtual void ApplyEffectEffect() { }
    public virtual void ApplyEffectEffect(EffectId eid) { }
    public virtual void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }

    public virtual void SetNextState()
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
        }
        else
        {
            State = State.Attack;
            SyncPosAndDir();
        }
    }

    public virtual void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHp();
            // 부활 Effect 추가
        }
    }
    
    protected virtual void SyncPosAndDir()
    {
        if (Room == null || Target == null) return;
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        Room.Broadcast(new S_Sync { ObjectId = Id, PosInfo = PosInfo });
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        var skillName = skill.ToString();
        var name = UnitId.ToString();
        if (skillName.Contains(name) == false) return;
        NewSkill = skill;
        SkillList.Add(NewSkill);
    }

    protected virtual void SkillInit()
    {
        var skillUpgradedList = Player.SkillUpgradedList;
        var name = UnitId.ToString();
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            var skillName = skill.ToString();
            if (skillName.Contains(name)) SkillList.Add(skill);
        }
        
        if (SkillList.Count == 0) return;
        foreach (var skill in SkillList) NewSkill = skill;
    }
    
    protected virtual State GetRandomState(State state1, State state2)
    {
        return new Random().Next(2) == 0 ? state1 : state2;
    }
}