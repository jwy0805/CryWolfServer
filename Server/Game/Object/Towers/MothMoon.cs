using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothMoon : MothLuna
{
    private int _debuffRemoveProb = 40;
    private bool _debuffSheep = false;
    private bool _healSheep = false;
    private bool _output = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothMoonRemoveDebuffSheep:
                    _debuffSheep = true;
                    break;
                case Skill.MothMoonHealSheep:
                    _healSheep = true;
                    break;
                case Skill.MothMoonRange:
                    AttackRange += 3;
                    break;
                case Skill.MothMoonOutput:
                    _output = _debuffSheep;
                    break;
                case Skill.MothMoonAttackSpeed:
                    AttackSpeed += 0.15f;
                    break;
            }
        }
    }

    public override void RunSkill()
    {
        if (_debuffSheep)
        {
            BuffManager.Instance.RemoveAllBuff(this);
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