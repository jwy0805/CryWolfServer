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
                case Skill.SnakeletAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.2f;
                    break;
                case Skill.SnakeletAttack:
                    Attack += 6;
                    break;
                case Skill.SnakeletEvasion:
                    Evasion += 15;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        AttackSpeedReciprocal = 5 / 6f;
        AttackSpeed *= AttackSpeedReciprocal;
    }
}