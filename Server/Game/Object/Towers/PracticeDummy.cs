using Google.Protobuf.Protocol;

namespace Server.Game;

public class PracticeDummy : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PracticeDummyHealth:
                    MaxHp += 40;
                    Hp += 40;
                    break;
                case Skill.PracticeDummyHealth2:
                    MaxHp += 60;
                    Hp += 60;
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