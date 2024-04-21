using Google.Protobuf.Protocol;

namespace Server.Game;

public class Lurker : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                
            }
        }
    }
}