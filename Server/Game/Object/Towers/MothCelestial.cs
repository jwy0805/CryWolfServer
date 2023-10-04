using Google.Protobuf.Protocol;

namespace Server.Game;

public class MothCelestial : MothMoon
{
    private bool _sheepHealth = false;
    private bool _breedSheep = false;
    private readonly int _healthParam = 100;
    private int _debuffRemoveProb = 75;
    private int _breedProb = 3;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothCelestialSheepHealth:
                    _sheepHealth = true;
                    break;
                case Skill.MothCelestialGroundAttack:
                    AttackType = 2;
                    break;
                case Skill.MothCelestialAccuracy:
                    Accuracy += 10;
                    break;
                case Skill.MothCelestialPoisonResist:
                    PoisonResist += 15;
                    break;
                case Skill.MothCelestialFireResist:
                    FireResist += 15;
                    break;
                case Skill.MothCelestialPoison:
                    Room?.Broadcast(new S_SkillUpdate { 
                        ObjectEnumId = (int)TowerId, 
                        ObjectType = GameObjectType.Monster, 
                        SkillType = SkillType.SkillProjectile 
                    });
                    break;
                case Skill.MothCelestialBreedSheep:
                    _breedSheep = true;
                    break;
            }
        }
    }

    public override void RunSkill()
    {
        if (_sheepHealth) BuffManager.Instance.AddBuff(BuffId.HealthIncrease, this, _healthParam);
        
    }
}