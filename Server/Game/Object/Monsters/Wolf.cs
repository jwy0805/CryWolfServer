using Google.Protobuf.Protocol;

namespace Server.Game;

public class Wolf : WolfPup
{
    private bool _drain = false;
    private float _drainParam = 0.25f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfDefence:
                    Defence += 4;
                    TotalDefence += 4;
                    break;
                case Skill.WolfDrain:
                    _drain = true;
                    break;
                case Skill.WolfAvoid:
                    Evasion += 10;
                    TotalEvasion += 10;
                    break;
                case Skill.WolfFireResist:
                    FireResist += 10;
                    TotalFireResist += 10;
                    break;
                case Skill.WolfPoisonResist:
                    PoisonResist += 10;
                    TotalPoisonResist += 10;
                    break;
                case Skill.WolfDna:
                    break;
            }
        }
    }
}