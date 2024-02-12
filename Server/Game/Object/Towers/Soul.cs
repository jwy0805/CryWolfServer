using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Soul : Tower
{
    private bool _drain = false;
    protected readonly float DrainParam = 0.2f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SoulAttack:
                    Attack += 8;
                    break;
                case Skill.SoulDefence:
                    Defence += 5;
                    break;
                case Skill.SoulHealth:
                    MaxHp += 25;
                    Hp += 25;
                    break;
                case Skill.SoulDrain:
                    _drain = true;
                    break;
            }
        }
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindNearestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
        BroadcastDest();
        
        State = State.Moving;
        BroadcastMove();
    }

    // protected override void UpdateMoving()
    // {
    //     // Targeting
    //     Target = Room?.FindNearestTarget(this);
    //     if (Target != null)
    //     {
    //         DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
    //         (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos, false);
    //         BroadcastDest();
    //     }
    //     
    //     if (Target == null || Target.Room != Room)
    //     {
    //         State = State.Idle;
    //         BroadcastMove();
    //         return;
    //     }
    //
    //     if (Room != null)
    //     {
    //         // 이동
    //         // target이랑 너무 가까운 경우
    //         // Attack
    //         StatInfo targetStat = Target.Stat;
    //         Vector3 position = CellPos;
    //         if (targetStat.Targetable)
    //         {
    //             float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
    //             double deltaX = DestPos.X - CellPos.X;
    //             double deltaZ = DestPos.Z - CellPos.Z;
    //             Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
    //             if (distance <= AttackRange)
    //             {
    //                 CellPos = position;
    //                 State = State.Attack;
    //                 BroadcastMove();
    //                 return;
    //             }
    //         }
    //         
    //         BroadcastMove();
    //     }
    // }

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
                    (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
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

    public override void SetNormalAttackEffect(GameObject target)
    {
        if (!_drain) return;
        Hp += (int)((TotalAttack - target.TotalDefence) * DrainParam);
        Room?.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
    }
}