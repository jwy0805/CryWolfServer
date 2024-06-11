using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Burrow : Monster
{
    private bool _halfBurrow = false;
    
    protected long IdleToRushAnimTime;
    protected long RushToIdleAnimTime;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BurrowHealth:
                    MaxHp += 30;
                    Hp += 30;
                    BroadcastHp();
                    break;
                case Skill.BurrowDefence:
                    Defence += 2;
                    break;
                case Skill.BurrowEvasion:
                    Evasion += 10;
                    break;
                case Skill.BurrowHalfBurrow:
                    _halfBurrow = true;
                    break;
            }
        }
    }

    public override State State
    {
        get => PosInfo.State;
        set
        {
            var preState = PosInfo.State;
            PosInfo.State = value;
            BroadcastState();
            StateChanged = preState != PosInfo.State;
        }
    }
    
    public override void Init()
    {
        base.Init();
        IdleToRushAnimTime = StdAnimTime * 2 / 3;
        RushToIdleAnimTime = StdAnimTime * 5 / 6;
        Player.SkillSubject.SkillUpgraded(Skill.BurrowHalfBurrow);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
            UpdateSkill();
            Mp = 0;
        }
        else
        {
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
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.IdleToRush:
                    UpdateIdleToRush();
                    break;
                case State.RushToIdle:
                    UpdateRushToIdle();
                    break;
                case State.IdleToUnderground:
                    UpdateIdleToUnderground();
                    break;
                case State.UndergroundToIdle:
                    UpdateUndergroundToIdle();
                    break;
                case State.Underground:
                    UpdateUnderground();
                    break;
                case State.Faint:
                    break;
            }   
        }
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this); 
        if (Target == null) return;
        State = _halfBurrow ? State.IdleToRush : State.Moving;
    }

    protected override void UpdateRush()
    {   // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = Vector3.Distance(DestPos, CellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance <= TotalAttackRange)
        {
            State = State.RushToIdle;
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected virtual void UpdateUnderground() { }

    protected virtual void UpdateIdleToRush()
    {
        MotionChangeEvents(IdleToRushAnimTime);
    }
    
    protected virtual void UpdateRushToIdle()
    {
        MotionChangeEvents(RushToIdleAnimTime);   
    }
    
    protected virtual void UpdateIdleToUnderground() { }
    protected virtual void UpdateUndergroundToIdle() { }

    protected override async void MotionChangeEvents(long animTime)
    {
        await Scheduler.ScheduleEvent(animTime, () => SetNextState(State));
    }

    public override void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHp();
            // 부활 Effect 추가
        }
        
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            if (StateChanged) EvasionParam += 20;
        }
        
        if (state == State.RushToIdle)
        {
            State = State.Attack;
            SetDirection();
            if (StateChanged) EvasionParam -= 20;
        }
    }
}