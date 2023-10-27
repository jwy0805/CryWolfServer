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
                case Skill.LurkerDefence:
                    Defence += 4;
                    break;
                case Skill.LurkerHealth:
                    MaxHp += 25;
                    Hp += 25;
                    BroadcastHealth();
                    break;
                case Skill.LurkerHealth2:
                    MaxHp += 20;
                    Hp += 20;
                    BroadcastHealth();
                    break;
                case Skill.LurkerSpeed:
                    MoveSpeed += 2f;
                    TotalMoveSpeed += 2f;
                    break;
            }
        }
    }
}