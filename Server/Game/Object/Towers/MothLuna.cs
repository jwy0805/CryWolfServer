using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothLuna : Tower
{
    protected Vector3 StartCell;
    protected long LastSetDest = 0;

    private bool _faint = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothLunaAttack:
                    Attack += 3;
                    break;
                case Skill.MothLunaAccuracy:
                    Accuracy += 5;
                    break;
                case Skill.MothLunaFaint:
                    _faint = true;
                    break;
                case Skill.MothLunaSpeed:
                    MoveSpeed += 2;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        StartCell = CellPos;
    }
    
    protected override void UpdateIdle()
    {
        GameObject? target = Room.FindMosquitoInFence();
        if (target == null) return;
        Target ??= target;
        if (Target is { Targetable: true })
        {
            DestPos = Target.CellPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            State = State.Moving;
            BroadcastMove();
        }
    }
    
    protected override void UpdateMoving()
    {
        if (Target is { Targetable: true })
        {
            // Attack
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = State.Attack;
                    BroadcastMove();
                    return;
                }
            }
        }
        else
        {
            // Targeting
            double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
            if (timeNow > LastSearch + SearchTick)
            {
                LastSearch = timeNow;
                GameObject? target = Room?.FindNearestTarget(this);
                Target ??= target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                    return;
                }
            }

            DestPos = StartCell;
            BroadcastDest();
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(
                DestPos with { Y = 0 } - CellPos with { Y = 0 })); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= 0.5f)
            {
                State = State.Idle;
                BroadcastMove();
            }
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;

        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
        }
        else
        {
            if (Target.Hp > 0)
            {
                Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = State.Attack;
                    SetDirection();
                }
                else
                {
                    DestPos = Target.CellPos;
                    (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                    State = State.Moving;
                }
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}