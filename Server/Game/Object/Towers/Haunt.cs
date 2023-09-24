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
            switch (Skill)
            {
                case Skill.HauntLongAttack:
                    _longAttack = true;
                    AttackRange += 3.5f;
                    break;
                case Skill.HauntAttackSpeed:
                    AttackSpeed += 0.15f;
                    break;
                case Skill.HauntAttack:
                    Attack += 10;
                    break;
                case Skill.HauntFireResist:
                    FireResist += 10;
                    break;
                case Skill.HauntPoisonResist:
                    PoisonResist += 10;
                    break;
                case Skill.HauntFire:
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
                    State = _longAttack == true ? State.Skill : State.Attack;
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