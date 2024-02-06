using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Bloom : Bud
{
    private bool _combo = false;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BloomAttack:
                    Attack += 10;
                    break;
                case Skill.BloomRange:
                    AttackRange += 2;
                    break;
                case Skill.BloomAttackSpeed:
                    AttackSpeed += 0.15f;
                    break;
                case Skill.BloomAttackSpeed2:
                    AttackSpeed += 0.2f;
                    break;
                case Skill.Bloom3Combo:
                    _combo = true;
                    Attack -= 16;
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)TowerId,
                        ObjectType = GameObjectType.Tower,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
                case Skill.BloomAirAttack:
                    AttackType = 2;
                    break;
            }
        }
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindNearestTarget(this);
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        if (Target == null) return;

        StatInfo targetStat = Target.Stat;
        if (targetStat.Targetable)
        {
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                State = _combo ? State.Skill : State.Attack;
                BroadcastMove();
                // Room?.Broadcast(new S_State { ObjectId = Id, State = State });
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
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = _combo ? State.Skill : State.Attack;
                    SetDirection();
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