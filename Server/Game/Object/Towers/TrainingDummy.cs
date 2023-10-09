using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class TrainingDummy : TargetDummy
{
    private bool _faint = false;
    private bool _debuffRemove = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.TrainingDummyAggro:
                    SkillRange += 4.0f;
                    break;
                case Skill.TrainingDummyDefence:
                    Defence += 6;
                    break;
                case Skill.TrainingDummyHeal:
                    HealParam += 0.1f;
                    break;
                case Skill.TrainingDummyHealth:
                    MaxHp += 200;
                    Hp += 200;
                    break;
                case Skill.TrainingDummyFireResist:
                    FireResist += 15;
                    break;
                case Skill.TrainingDummyPoisonResist:
                    PoisonResist += 15;
                    break;
                case Skill.TrainingDummyFaint:
                    _faint = true;
                    break;
                case Skill.TrainingDummyDebuffRemove:
                    _debuffRemove = true;
                    break;
            }
        }
    }

    public override void SetNormalAttackEffect(GameObject master)
    {
        if (_faint == true && Target != null) Target.State = State.Faint;
    }
    
    public override void RunSkill()
    {
        base.RunSkill();
        if (_debuffRemove == true) BuffManager.Instance.RemoveAllBuff(this);
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
                    State = State.Attack;
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