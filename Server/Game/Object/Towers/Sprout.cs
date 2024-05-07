using Google.Protobuf.Protocol;

namespace Server.Game;

public class Sprout : Tower
{
    private bool _drain = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SproutDrain:
                    _drain = true;
                    break;
                case Skill.SproutFireAttack:
                    break;
                case Skill.SproutFireResist:
                    break;
            }
        }
    }
}