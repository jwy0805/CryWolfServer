using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Bud : Tower
{
    private bool _seed = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BudAttack:
                    Attack += 5;
                    break;
                case Skill.BudAttackSpeed:
                    AttackSpeed += 0.1f;
                    break;
                case Skill.BudRange:
                    AttackRange += 2.0f;
                    break;
                case Skill.BudSeed:
                    _seed = true;
                    break;
                case Skill.BudDouble:
                    Attack -= 3;
                    AttackSpeed -= 0.1f;
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)TowerId,
                        ObjectType = GameObjectType.Tower,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
            }
        }
    }
    
    protected override void UpdateIdle()
    {
        GameObject? target = Room?.FindNearestTarget(this);
        if (target == null) return;
        LastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        Target ??= target;
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
                State = _seed ? State.Skill : State.Attack;
                BroadcastMove();
                Room?.Broadcast(new S_State { ObjectId = Id, State = State });
            }
        }
    }

    protected override void UpdateSkill()
    {
        base.UpdateAttack();
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
                    State = _seed == true ? State.Skill : State.Attack;
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