using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Haunt : Soul
{
    private bool _longAttack = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.HauntLongAttack:
            //         _longAttack = true;
            //         AttackRange += 3.5f;
            //         break;
            //     case Skill.HauntAttackSpeed:
            //         AttackSpeed += 0.15f;
            //         break;
            //     case Skill.HauntAttack:
            //         Attack += 10;
            //         break;
            //     case Skill.HauntFireResist:
            //         FireResist += 10;
            //         break;
            //     case Skill.HauntPoisonResist:
            //         PoisonResist += 10;
            //         break;
            //     case Skill.HauntFire:
            //         Room?.Broadcast(new S_SkillUpdate
            //         {
            //             ObjectEnumId = (int)TowerId,
            //             ObjectType = GameObjectType.Tower,
            //             SkillType = SkillType.SkillProjectile
            //         });
            //         break;
            // }
        }
    }
    
    protected override void UpdateMoving()
    {
        // Targeting
        Target = Room?.FindClosestTarget(this);
        if (Target != null)
        {
            DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
        }
        
        if (Target == null || Target.Room != Room)
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
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = _longAttack ? State.Skill : State.Attack;
                    BroadcastPos();
                    return;
                }
            }
            
            BroadcastPos();
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
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = _longAttack ? State.Skill : State.Attack;
                    SyncPosAndDir();
                }
                else
                {
                    State = State.Idle;
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