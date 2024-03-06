using Google.Protobuf.Protocol;

namespace Server.Game;

public class MothCelestial : MothMoon
{
    private bool _sheepHealth = false;
    private bool _breedSheep = false;
    private readonly int _healthParam = 100;
    private readonly int _breedProb = 10;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.MothCelestialSheepHealth:
            //         _sheepHealth = true;
            //         break;
            //     case Skill.MothCelestialGroundAttack:
            //         AttackType = 2;
            //         break;
            //     case Skill.MothCelestialAccuracy:
            //         Accuracy += 10;
            //         break;
            //     case Skill.MothCelestialPoisonResist:
            //         PoisonResist += 15;
            //         break;
            //     case Skill.MothCelestialFireResist:
            //         FireResist += 15;
            //         break;
            //     case Skill.MothCelestialPoison:
            //         Room?.Broadcast(new S_SkillUpdate { 
            //             ObjectEnumId = (int)TowerId, 
            //             ObjectType = GameObjectType.Monster, 
            //             SkillType = SkillType.SkillProjectile 
            //         });
            //         break;
            //     case Skill.MothCelestialBreedSheep:
            //         _breedSheep = true;
            //         break;
            // }
        }
    }

    public override void RunSkill()
    {
        if (Room == null) return;
        List<GameObject> sheeps = Room.FindTargets(this,
            new List<GameObjectType> { GameObjectType.Sheep }, AttackRange);
        if (sheeps.Any())
        {
            foreach (var gameObject in sheeps)
            {
                if (gameObject is not Sheep sheep) continue;
                sheep.Hp += HealParam;
                Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                BuffManager.Instance.RemoveAllDebuff(sheep);
                sheep.YieldIncrement = sheep.Resource * OutputParam / 100; 
                if (_sheepHealth) BuffManager.Instance.AddBuff(BuffId.HealthIncrease, sheep, this, _healthParam);
            }
        }

        Random random = new Random();
        if (_breedSheep)
        {
            int r = random.Next(99);
            if (r < _breedProb)
            {
                Map map = Room.Map;
                Sheep sheep = ObjectManager.Instance.Add<Sheep>();
                sheep.CellPos = map.FindSpawnPos(sheep);
                sheep.Info.PosInfo = sheep.PosInfo;
                sheep.Player = Player;
                sheep.Init();
                Room.Push(Room.EnterGame, sheep);
            }
        }
    }
}