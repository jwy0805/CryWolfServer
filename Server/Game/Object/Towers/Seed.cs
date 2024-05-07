using Google.Protobuf.Protocol;

namespace Server.Game;

public class Seed : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SeedEvasion:
                    Evasion += 10;
                    break;
                case Skill.SeedRange:
                    AttackRange += 1;
                    break;
            }
        }
    }
}