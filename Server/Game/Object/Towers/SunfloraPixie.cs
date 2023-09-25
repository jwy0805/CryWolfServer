using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunfloraPixie : SunflowerFairy
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
        }
    }
}