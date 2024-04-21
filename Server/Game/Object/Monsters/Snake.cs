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
                case Skill.SnakeFire:
                    Room?.Broadcast(new S_SkillUpdate { 
                        ObjectEnumId = (int)UnitId, 
                        ObjectType = GameObjectType.Monster, 
                        SkillType = SkillType.SkillProjectile 
                    });
                    break;
                case Skill.SnakeAccuracy:
                    Accuracy += 25;
                    break;
                case Skill.SnakeFireResist:
                    FireResist += 20;
                    break;
                case Skill.SnakeSpeed:
                    MoveSpeed += 1;
                    break;
            }
        }
    }
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 5f);
    }
}