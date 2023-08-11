using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snake : Monster
{
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
                    Room?.Broadcast(new S_SkillUpgrade
                        { SkillType = SkillType.SkillProjectile, MonsterId = MonsterId });
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Snake;
    }
}