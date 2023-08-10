using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snake : Monster
{
    private bool _fire = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeAttackSpeed:
                    Stat.AttackSpeed += 0.15f;
                    break;
                case Skill.SnakeAttack:
                    Stat.Attack += 10;
                    break;
                case Skill.SnakeRange:
                    Stat.AttackRange += 2;
                    break;
                case Skill.SnakeAccuracy:
                    Stat.Accuracy += 10;
                    break;
                case Skill.SnakeFire:
                    _fire = true;
                    break;
            }
        }
    }
}