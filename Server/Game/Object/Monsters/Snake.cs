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
                case Skill.SnakeAccuracy:
                    Accuracy += 10;
                    break;
                case Skill.SnakeFire:
                    Attack += 10;
                    Room?.Broadcast(new S_SkillUpdate { 
                            ObjectEnumId = (int)UnitId, 
                            ObjectType = GameObjectType.Monster, 
                            SkillType = SkillType.SkillProjectile 
                        });
                    break;
            }
        }
    }
}