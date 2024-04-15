using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoBug : Monster
{
    protected List<GameObjectType> _typeList = new() { GameObjectType.Sheep };
    private bool _woolDown = false;
    protected int WoolDownRate = 10;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.MosquitoBugAvoid:
            //         Evasion += 10;
            //         break;
            //     case Skill.MosquitoBugDefence:
            //         Defence += 2;
            //         break;
            //     case Skill.MosquitoBugSpeed:
            //         MoveSpeed += 1.0f;
            //         break;
            //     case Skill.MosquitoBugWoolDown:
            //         _woolDown = true;
            //         break;
            // }
        }
    }

    protected override void UpdateIdle()
    {
        GameObject? target;
        if (Target == null || Target.Targetable == false)
        {
            target = Room.FindClosestTarget(this, _typeList, 2) 
                     ?? Room.FindClosestTarget(this, 2);
            LastSearch = Room.Stopwatch.Elapsed.Milliseconds;
            if (target == null) return;
            Target = target;
        }

        if (Target == null) return;
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        
        State = State.Moving;
        BroadcastPos();
    }

    protected override void UpdateMoving()
    {
        // Targeting
        double timeNow = Room.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            Target = Room.FindClosestTarget(this, _typeList, AttackType) 
                                 ?? Room.FindClosestTarget(this, AttackType);
            if (Target != null)
            {
                DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                BroadcastDest();
            }
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            // Attack
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3()
                    .SqrMagnitude(DestPos with { Y = 0 } - CellPos with { Y = 0 })); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = State.Attack;
                    BroadcastPos();
                    return;
                }
            }
            
            BroadcastPos();
        }
    }
    
    // protected override void UpdateMoving()
    // {
    //     // Targeting
    //     double timeNow = Room.Stopwatch.Elapsed.TotalMilliseconds;
    //     if (timeNow > LastSearch + SearchTick)
    //     {
    //         LastSearch = timeNow;
    //         GameObject? target = Room.FindNearestTarget(this, _typeList, AttackType) 
    //                              ?? Room.FindNearestTarget(this, AttackType);
    //         if (Target?.Id != target?.Id)
    //         {
    //             Target = target;
    //             if (Target != null)
    //             {
    //                 DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
    //                 (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
    //                 BroadcastDest();
    //             }
    //         }
    //     }
    //     
    //     if (Target == null || Target.Targetable == false || Target.Room != Room)
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
    //             float distance = (float)Math.Sqrt(new Vector3()
    //                 .SqrMagnitude(DestPos with { Y = 0 } - CellPos with { Y = 0 })); // 거리의 제곱
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

    public override void SetNormalAttackEffect(GameObject target)
    {
        if (_woolDown && target is Sheep sheep)
        {
            sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
        }
    }
}