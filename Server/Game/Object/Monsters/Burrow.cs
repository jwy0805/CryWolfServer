using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Burrow : Monster
{
    private bool _halfBurrow = false;
    
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
                    BroadcastHealth();
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

    public override void Init()
    {
        base.Init();
        AttackSpeedReciprocal = 2 / 3f;
        AttackSpeed *= AttackSpeedReciprocal;
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        
        State = _halfBurrow ? State.IdleToRush : State.Moving;
        BroadcastPos();
    }
    
    protected override void UpdateMoving()
    {
        if (_halfBurrow)
        {
            State = State.Rush;
            BroadcastPos();
            return;
        }
        
        base.UpdateMoving();
    }

    protected override void UpdateRush()
    {
        // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target != null)
        {   
            // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
            Vector3 position = CellPos;
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                CellPos = position;
                State = State.RushToIdle;
                BroadcastPos();
                return;
            }
            
            // Target이 있으면 이동
            DestPos = Room.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            BroadcastPos();
        }
    }

    public override void SetNextState(State state)
    {
        base.SetNextState(state);
        
        if (state == State.IdleToRush)
        {
            State = State.Rush;
            BroadcastPos();
        }

        if (state == State.RushToIdle)
        {
            State = State.Attack;
            BroadcastPos();
        }
    }
}