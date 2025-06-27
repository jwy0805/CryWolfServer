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
                    MaxHp += 40;
                    Hp += 40;
                    break;
                case Skill.CactiDefence2:
                    Defence += 2;
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