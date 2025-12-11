using Google.Protobuf.Protocol;
using Server.Data;

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
                case Skill.LurkerSpeed:
                    MoveSpeed += DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.LurkerMagicalDefence:
                    MagicalDefence += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.LurkerPoisonResist:
                    PoisonResist += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
    }
}