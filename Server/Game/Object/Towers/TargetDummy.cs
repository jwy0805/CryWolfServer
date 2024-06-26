using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class TargetDummy : PracticeDummy
{
    private bool _heal = false;
    protected float HealParam = 0.15f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.TargetDummyHealSelf:
                    _heal = true;
                    break;
                case Skill.TargetDummyPoisonResist:
                    PoisonResist += 10;
                    break;
                case Skill.TargetDummyAggro:
                    Reflection = true;
                    ReflectionRate = 0.1f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
        
        Player.SkillSubject.SkillUpgraded(Skill.TargetDummyHealSelf);
        Player.SkillSubject.SkillUpgraded(Skill.TargetDummyPoisonResist);
        Player.SkillSubject.SkillUpgraded(Skill.TargetDummyAggro);
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
            case State.Idle:
                UpdateIdle();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Skill:
                UpdateSkill();
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
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance > AttackRange) return;
        State = _heal && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void UpdateSkill()
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
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        SkillImpactEvents(0);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnEffect(EffectId.StateHeal, this, PosInfo, true);
            Hp += (int)(MaxHp * HealParam);
            Mp = 0;
        });
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
        
        State = _heal && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}