using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Creeper : Lurker
{
    protected double CrashTime;
    protected long RollCoolTime;
    protected bool Start = false;
    private bool _roll = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CreeperAttack:
                    Attack += 10;
                    TotalAttack += 10;
                    break;
                case Skill.CreeperSpeed:
                    MoveSpeed += 2.5f;
                    TotalMoveSpeed += 2.5f;
                    break;
                case Skill.CreeperAttackSpeed:
                    AttackSpeed += 0.15f;
                    TotalAttackSpeed += 0.15f;
                    break;
                case Skill.CreeperRoll:
                    _roll = true;
                    break;
                case Skill.CreeperPoison:
                    Room?.Broadcast(new S_SkillUpdate { 
                        ObjectEnumId = (int)MonsterId, 
                        ObjectType = GameObjectType.Monster, 
                        SkillType = SkillType.SkillProjectile 
                    });
                    break;
            }
        }
    }
    
    protected override void UpdateMoving()
    {
        if (_roll & Start == false)
        {
            State = State.Rush;
            Start = true;
            BroadcastMove();
        }
        else
        {
            // Targeting
            double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
            if (timeNow > LastSearch + SearchTick)
            {
                LastSearch = timeNow;
                GameObject? target = Room?.FindNearestTarget(this);
                if (Target?.Id != target?.Id)
                {
                    Target = target;
                    if (Target != null)
                    {
                        DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                        (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                        BroadcastDest();
                    }
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

            BroadcastMove();
        }
    }

    protected override void UpdateRush()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindNearestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                // Roll 충돌 처리
                if (distance <= Stat.SizeX * 0.25 + 0.75f)
                {
                    CellPos = position;
                    Target.OnDamaged(this, SkillDamage);
                    Mp += MpRecovery;
                    State = State.KnockBack;
                    DestPos = CellPos + (-Vector3.Normalize(Target.CellPos - CellPos) * 3);
                    BroadcastMove();
                    Room.Broadcast(new S_SetKnockBack
                    {
                        ObjectId = Id, 
                        Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                    });
                    return;
                }
            }
        }

        BroadcastMove();
    }

    protected override void UpdateKnockBack()
    {
        // 넉백중 충돌하면 Idle
        //
    }
}