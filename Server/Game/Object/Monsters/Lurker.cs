using Google.Protobuf.Protocol;

namespace Server.Game;

public class Lurker : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.LurkerSpeed:
                    MoveSpeed += 1;
                    break;
                case Skill.LurkerDefence:
                    Defence += 3;
                    break;
                case Skill.LurkerPoisonResist:
                    PoisonResist += 15;
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