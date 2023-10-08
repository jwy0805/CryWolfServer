using Google.Protobuf.Protocol;

namespace Server.Game;

public sealed class ObjectManager : IFactory
{
    public static ObjectManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<int, Player?> _players = new();
    
    // [UNUSED(1)][TYPE(7)][ID(24)]
    private int _counter = 0;

    private readonly Dictionary<TowerId, Type?> _towerDict = new()
    {
        { TowerId.Bud, typeof(Bud) },
        { TowerId.Bloom, typeof(Bloom) },
        { TowerId.Blossom, typeof(Blossom) },
        { TowerId.PracticeDummy, typeof(PracticeDummy) },
        { TowerId.TargetDummy, typeof(TargetDummy) },
        { TowerId.TrainingDummy, typeof(TrainingDummy) },
        { TowerId.SunBlossom, typeof(SunBlossom) },
        { TowerId.SunflowerFairy, typeof(SunflowerFairy) },
        { TowerId.SunfloraPixie, typeof(SunfloraPixie) },
        { TowerId.MothLuna, typeof(MothLuna) },
        { TowerId.MothMoon, typeof(MothMoon) },
        { TowerId.MothCelestial, typeof(MothCelestial) },
        { TowerId.Soul, typeof(Soul) },
        { TowerId.Haunt, typeof(Haunt) },
        { TowerId.SoulMage, typeof(SoulMage) }
    };

    private readonly Dictionary<MonsterId, Type?> _monsterDict = new()
    {
        { MonsterId.WolfPup, typeof(WolfPup) },
        { MonsterId.Wolf, typeof(Wolf) },
        { MonsterId.Werewolf, typeof(Werewolf) },
        { MonsterId.Lurker, typeof(Lurker) },
        { MonsterId.Creeper, typeof(Creeper) },
        { MonsterId.Horror, typeof(Horror) },
        { MonsterId.Snakelet, typeof(Snakelet) },
        { MonsterId.Snake, typeof(Snake) },
        { MonsterId.SnakeNaga, typeof(SnakeNaga) },
        { MonsterId.Shell, typeof(Shell) },
        { MonsterId.Spike, typeof(Spike) },
        { MonsterId.Hermit , typeof(Hermit) },
        { MonsterId.MosquitoBug, typeof(MosquitoBug) },
        { MonsterId.MosquitoPester, typeof(MosquitoPester) },
        { MonsterId.MosquitoStinger, typeof(MosquitoStinger) }
    };
    
    private readonly Dictionary<ProjectileId, Type?> _projectileDict = new()
    {
        { ProjectileId.BasicAttack, typeof(BasicAttack) },
        { ProjectileId.SmallFire, typeof(SmallFire) },
        { ProjectileId.BigFire, typeof(BigFire) },
        { ProjectileId.PoisonAttack, typeof(PoisonAttack) },
        { ProjectileId.BigPoison, typeof(BigPoison) },
        { ProjectileId.Seed, typeof(Seed) },
        { ProjectileId.BlossomSeed, typeof(BlossomSeed) },
        { ProjectileId.BlossomArrow, typeof(BlossomArrow) },
        { ProjectileId.HauntArrow, typeof(HauntArrow) },
        { ProjectileId.HauntFireAttack, typeof(HauntFireAttack) },
        { ProjectileId.SoulMageAttack, typeof(SoulMageAttack) },
        { ProjectileId.SoulMagePunch, typeof(SoulMagePunch) },
        { ProjectileId.SunfloraPixieArrow, typeof(SunfloraPixieArrow) },
        { ProjectileId.SunfloraPixieFire, typeof(SunfloraPixieFire) },
        { ProjectileId.MothMoonAttack, typeof(MothMoonAttack)},
        { ProjectileId.MothCelestialPoisonAttack, typeof(MothCelestialPoisonAttack) }
    };

    private readonly Dictionary<EffectId, Type?> _effectDict = new()
    {
        { EffectId.LightningStrike, typeof(Effect) },
        { EffectId.PoisonBelt, typeof(PoisonBelt) }
    };

    public Tower CreateTower(TowerId towerId)
    {
        return Create(_towerDict, towerId) as Tower ?? throw new InvalidOperationException();
    }

    public Monster CreateMonster(MonsterId monsterId)
    {
        return Create(_monsterDict, monsterId) as Monster ?? throw new InvalidOperationException();
    }
    
    public Projectile CreateProjectile(ProjectileId projectileId)
    {
        return Create(_projectileDict, projectileId) as Projectile ?? throw new InvalidOperationException();
    }

    public Effect CreateEffect(EffectId effectId)
    {
        return Create(_effectDict, effectId) as Effect ?? throw new InvalidOperationException();
    }

    private GameObject Create<T>(Dictionary<T, Type?> dict, T key) where T : Enum
    {
        if (!dict.TryGetValue(key, out Type? type))
            throw new ArgumentException($"Invalid {nameof(T)}");

        GameObject entity = (GameObject)Activator.CreateInstance(type!)!;
        lock (_lock) entity.Id = GenerateId(entity.ObjectType);
        
        return entity;
    }
    
    public T Add<T>() where T : GameObject, new()
    {
        T gameObject = new T();

        lock (_lock)
        {
            gameObject.Id = GenerateId(gameObject.ObjectType);

            if (gameObject.ObjectType == GameObjectType.Player)
            {
                _players.Add(gameObject.Id, gameObject as Player);
            }
        }

        return gameObject;
    }

    public int GenerateId(GameObjectType type)
    {
        lock (_lock)
        {
            return ((int)type << 24) | _counter++;
        }
    }

    public static GameObjectType GetObjectTypeById(int id)
    {
        int type = (id >> 24) & 0x7F;
        return (GameObjectType)type;
    }

    public bool Remove(int objectId)
    {
        GameObjectType objectType = GetObjectTypeById(objectId);
        
        lock (_lock)
        {
            if (objectType == GameObjectType.Player) return _players.Remove(objectId);
        }

        return false;
    }

    public Player? Find(int objectId)
    {
        GameObjectType objectType = GetObjectTypeById(objectId);

        lock (_lock)
        {
            if (objectType == GameObjectType.Player)
            {
                if (_players.TryGetValue(objectId, out var player)) return player;
            }
        }

        return null;
    }
}