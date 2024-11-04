using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Server.Game;

public class Creature : GameObject
{
    protected virtual Skill NewSkill { get; set; }
    protected Skill Skill;
    protected readonly Scheduler Scheduler = new();
    protected readonly List<Skill> SkillList = new();
    protected bool StateChanged;
    protected bool AttackEnded = true;
    protected float AttackImpactMoment = 0.5f;
    protected float SkillImpactMoment = 0.5f;
    protected float SkillImpactMoment2 = 0.5f;
    protected Guid AttackTaskId;
    protected Guid EndTaskId;
    protected const long MpTime = 1000;
    protected const long StdAnimTime = 1000;
    
    public Action<BuffId, BuffParamType, GameObject, Creature, float, long, bool>? AddBuffAction { get; set; }
    public UnitId UnitId { get; set; }
    public Role UnitRole { get; protected set; }
    public virtual bool Degeneration { get; set; }

    public override State State
    {
        get => PosInfo.State;
        set
        {
            var preState = PosInfo.State;
            PosInfo.State = value;
            if (preState != PosInfo.State) DistRemainder = 0;

            switch (value)
            {
                case State.Attack:
                    OnAttack();
                    break;
                case State.Attack2:
                    OnAttack2();
                    break;
                case State.Attack3:
                    OnAttack3();
                    break;
                case State.Skill:
                    OnSkill();
                    break;
                case State.Skill2:
                    OnSkill2();
                    break;
                case State.Skill3:
                    OnSkill3();
                    break;
            }
            
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
    {
        if (Room == null) return;
        
        // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   
            // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(this, Target);
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
        if (Path.Count == 0)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }
        BroadcastPath();
    }
    
    protected virtual void UpdateAttack()
    {
        if (Room == null) return;

        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            if (AttackEnded) return;
            AttackEnded = true;
            Scheduler.CancelEvent(AttackTaskId);
            Scheduler.CancelEvent(EndTaskId);
            SetNextState();
        }
    }   
    
    protected virtual void UpdateAttack2() { }
    protected virtual void UpdateAttack3() { }

    protected virtual void UpdateSkill()
    {
        UpdateAttack();
    }
    
    protected virtual void UpdateSkill2() { }
    protected virtual void UpdateKnockBack() { }
    protected virtual void UpdateRush() { }
    protected virtual void UpdateDie() { }

    protected virtual void OnAttack()
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
        AttackImpactEvents(impactMoment);
        EndEvents(animPlayTime);
        AttackEnded = false;
    }

    protected virtual void OnAttack2()
    {
        OnAttack();
    }

    protected virtual void OnAttack3()
    {
        OnAttack();
    }
    
    protected virtual void OnSkill()
    {
        // It is based on the assumption that the skill is a buff skill.
        // If it is a skill that deals attack, you need to override it.
        // - Target null check, targetable check.
        // - AttackEnded check.
        if (Room == null || Hp <= 0) return;
        
        Room.Broadcast(new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        });
        var impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment);
        var animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        SkillImpactEvents(impactMoment);
        EndEvents(animPlayTime);
        AttackEnded = false;
    }

    protected virtual void OnSkill2()
    {
        OnSkill();
    }

    protected virtual void OnSkill3()
    {
        OnSkill();
    }
    
    public virtual void OnFaint()
    {
        State = State.Faint;
        AttackEnded = true;
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
    }
    
    protected virtual void AttackImpactEvents(long impactTime)
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

    protected virtual void SkillImpactEvents(long impactTime) { }

    protected virtual void MotionChangeEvents(long time) { }

    protected virtual void EndEvents(long animEndTime)
    {
        Scheduler.CancelEvent(EndTaskId);
        EndTaskId = Scheduler.ScheduleCancellableEvent(animEndTime, () =>
        {
            if (Room == null) return;
            if (Hp <= 0) return;
            SetNextState();
        });
    }
    
    protected virtual async void DieEvents(long standbyTime)
    {
        await Scheduler.ScheduleEvent(standbyTime, () =>
        {
            if (Room == null || State != State.Die) return;
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
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }
    public virtual void ApplyEffectEffect() { }
    public virtual void ApplyEffectEffect(EffectId eid) { }
    public virtual void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }

    protected virtual void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
        }
        else
        {
            State = State.Attack;
            SyncPosAndDir();
        }
    }

    public virtual void SetNextState(State state) { }
    
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

    public void ChangeTarget(GameObject target)
    {
        Target = target;
    }
    
    protected virtual State GetRandomState(State state1, State state2)
    {
        return new Random().Next(2) == 0 ? state1 : state2;
    }
}