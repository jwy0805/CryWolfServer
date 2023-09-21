using Google.Protobuf.Protocol;

namespace Server.Game;

public class WolfPup : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfPupAttack:
                    Attack += 5;
                    break;
                case Skill.WolfPupHealth:
                    Hp += 20;
                    MaxHp += 20;
                    break;
                case Skill.WolfPupSpeed:
                    MoveSpeed += 1.0f;
                    break;
                case Skill.WolfPupAttackSpeed:
                    AttackSpeed += 0.1f;
                    break;
            }
        }
    }
}