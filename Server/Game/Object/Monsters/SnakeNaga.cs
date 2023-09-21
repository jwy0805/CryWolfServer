using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnakeNaga : Snake
{
    private bool _drain = false;
    private bool _meteor = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeNagaAttack:
                    Attack += 10;
                    break;
                case Skill.SnakeNagaRange:
                    AttackRange += 2;
                    break;
                case Skill.SnakeNagaCritical:
                    CriticalChance += 25;
                    break;
                case Skill.SnakeNagaFireResist:
                    FireResist += 40;
                    break;
                case Skill.SnakeNagaDrain:
                    _drain = true;
                    break;
                case Skill.SnakeNagaMeteor:
                    _meteor = true;
                    break;
            }
        }
    }
}