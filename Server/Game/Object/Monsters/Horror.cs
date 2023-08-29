using Google.Protobuf.Protocol;

namespace Server.Game;

public class Horror : Creeper
{
    public bool PoisonStack = false;
    
    private bool _rollPoison = false;
    private bool _poisonBelt = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HorrorHealth:
                    MaxHp += 200;
                    Hp += 200;
                    break;
                case Skill.HorrorDefence:
                    Defence += 5;
                    TotalDefence += 5;
                    break;
                case Skill.HorrorPoisonResist:
                    PoisonResist += 15;
                    TotalPoisonResist += 15;
                    break;
                case Skill.HorrorPoisonStack:
                    PoisonStack = true;
                    break;
                case Skill.HorrorRollPoison:
                    _rollPoison = true;
                    break;
                case Skill.HorrorPoisonBelt:
                    _poisonBelt = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Horror;
        
    }
}