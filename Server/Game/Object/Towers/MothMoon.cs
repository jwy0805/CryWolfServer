using Google.Protobuf.Protocol;

namespace Server.Game;

public class MothMoon : MothLuna
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothMoonRemoveDebuffSheep:
                    break;
                case Skill.MothMoonHealSheep:
                    break;
                case Skill.MothMoonRange:
                    break;
                case Skill.MothMoonOutput:
                    break;
                case Skill.MothMoonAttackSpeed:
                    break;
            }
        }
    }
    
    
}