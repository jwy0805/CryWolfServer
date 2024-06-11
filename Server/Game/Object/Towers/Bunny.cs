using Google.Protobuf.Protocol;

namespace Server.Game;

public class Bunny : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BunnyHealth:
                    MaxHp += 20;
                    Hp += 20;
                    BroadcastHp();
                    break;
                case Skill.BunnyEvasion:
                    Evasion += 5;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
    }
}