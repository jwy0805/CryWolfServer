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
                case Skill.CactiHealth:
                    MaxHp += 30;
                    Hp += 30;
                    BroadcastHp();
                    break;
                case Skill.CactiHealth2:
                    Hp += 50;
                    MaxHp += 50;
                    BroadcastHp();
                    break;
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