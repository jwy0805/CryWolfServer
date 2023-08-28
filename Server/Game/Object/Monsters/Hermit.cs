using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hermit : Spike
{
    private bool _debuffRemove = false;
    private bool _aggro = false;
    private bool _faint = false;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HermitPoisonResist:
                    PoisonResist += 25;
                    break;
                case Skill.HermitFireResist:
                    FireResist += 25;
                    break;
                case Skill.HermitDebuffRemove:
                    _debuffRemove = true;
                    break;
                case Skill.HermitRange:
                    AttackRange += 2.0f;
                    break;
                case Skill.HermitAggro:
                    _aggro = true;
                    break;
                case Skill.HermitReflection:
                    Reflection = true;
                    break;
                case Skill.HermitFaint:
                    _faint = true;
                    break;
            }
        }
    }
        
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Hermit;
    }

    
}