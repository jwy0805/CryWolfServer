using Google.Protobuf.Protocol;

namespace Server.Game;

public class MoleRatKing : MoleRat
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MoleRatKingBurrow:
                    break;
                case Skill.MoleRatKingStealWool:
                    break;
            }
        }
    }
}