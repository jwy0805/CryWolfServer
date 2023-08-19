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
                    TotalAttack += 5;
                    break;
                case Skill.WolfPupHealth:
                    Hp += 20;
                    MaxHp += 20;
                    break;
                case Skill.WolfPupSpeed:
                    MoveSpeed += 1.0f;
                    TotalMoveSpeed += 1.0f;
                    break;
                case Skill.WolfPupAttackSpeed:
                    AttackSpeed += 0.1f;
                    TotalAttackSpeed += 0.1f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.WolfPup;
    }
}