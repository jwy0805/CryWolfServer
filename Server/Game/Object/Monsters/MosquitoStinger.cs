using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    private bool _longAttack = false;
    private bool _poison = false;
    private bool _sheepDeath = false;
    private bool _infection = false;

    public bool Poison => _poison;
    public bool Infection => _infection;
    public bool SheepDeath => _sheepDeath;
    public float DeathRate => 25;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.MosquitoStingerAvoid:
            //         Evasion += 15;
            //         break;
            //     case Skill.MosquitoStingerHealth:
            //         MaxHp += 60;
            //         Hp += 60;
            //         BroadcastHealth();
            //         break;
            //     case Skill.MosquitoStingerLongAttack:
            //         _longAttack = true;
            //         AttackRange += 3;
            //         break;
            //     case Skill.MosquitoStingerPoison:
            //         _poison = true;
            //         break;
            //     case Skill.MosquitoStingerPoisonResist:
            //         PoisonResist += 20;
            //         break;
            //     case Skill.MosquitoStingerInfection:
            //         _infection = true;
            //         break;
            //     case Skill.MosquitoStingerSheepDeath:
            //         _sheepDeath = true;
            //         break;
            // }
        }
    }
    
    protected override void UpdateMoving()
    {
        // Targeting
        double timeNow = Room.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            Target = Room.FindNearestTarget(this, _typeList, AttackType) 
                     ?? Room.FindNearestTarget(this, AttackType);
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
            BroadcastMove();
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
                    State = _longAttack ? State.Skill : State.Attack;
                    BroadcastMove();
                    return;
                }
            }
            
            BroadcastMove();
        }
    }
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        if (target is Sheep sheep)
        {
            sheep.YieldStop = true;
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
                    State = _longAttack ? State.Skill : State.Attack;
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