using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snakelet : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeletAttack:
                    Attack += 6;
                    break;
                case Skill.SnakeletRange:
                    AttackRange += 1.5f;
                    break;
                case Skill.SnakeletSpeed:
                    AttackSpeed += 0.1f;
                    break;
                case Skill.SnakeletAttackSpeed:
                    MoveSpeed += 2f;
                    break;
            }
        }
    }
}