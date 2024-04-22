using Google.Protobuf.Protocol;

namespace Server.Game;

public class Snake : Snakelet
{
    private bool _fire = false;
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeFire:
                    _fire = true;
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

    public override void SetProjectileEffect(GameObject target, ProjectileId pId = ProjectileId.None)
    {
        base.SetProjectileEffect(target, ProjectileId.SmallFire);
        if (_fire == false) return;
        BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 5f);
    }
}