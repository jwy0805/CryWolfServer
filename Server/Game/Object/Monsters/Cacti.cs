using Google.Protobuf.Protocol;

namespace Server.Game;

public class Cacti : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactiDefence:
                    Defence += 2;
                    break;
                case Skill.CactiDefence2:
                    Defence += 3;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
    }
}