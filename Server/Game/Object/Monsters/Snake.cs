using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snake : Snakelet
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
                    AttackSpeed += 0.15f;
                    TotalAttackSpeed += 0.15f;
                    break;
                case Skill.SnakeAttack:
                    Attack += 10;
                    TotalAttack += 10;
                    break;
                case Skill.SnakeRange:
                    AttackRange += 2;
                    break;
                case Skill.SnakeAccuracy:
                    Accuracy += 10;
                    TotalAccuracy += 10;
                    break;
                case Skill.SnakeFire:
                    Room?.Broadcast(new S_SkillUpdate
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