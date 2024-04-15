using Google.Protobuf.Protocol;

namespace Server.Game;

public class Burrow : Monster
{
    private bool _burrow = false;

    protected bool Start = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BurrowHealth:
                    Hp += 30;
                    MaxHp += 30;
                    break;
                case Skill.BurrowDefence:
                    Defence += 2;
                    break;
                case Skill.BurrowEvasion:
                    Evasion += 10;
                    break;
                case Skill.BurrowBurrow:
                    _burrow = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        
    }
}