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
        
    };

    private readonly Dictionary<MonsterId, Type?> _monsterDict = new()
    {
        { MonsterId.WolfPup, typeof(WolfPup) },
        { MonsterId.Snakelet, typeof(Snakelet) },
        { MonsterId.Snake, typeof(Snake) },
        { MonsterId.SnakeNaga, typeof(SnakeNaga) }
    };
    
    private readonly Dictionary<ProjectileId, Type?> _projectileDict = new()
    {
        { ProjectileId.BasicAttack, typeof(BasicAttack) }
    };

    private readonly Dictionary<EffectId, Type?> _effectDict = new()
    {
        
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
        if (!dict.TryGetValue(key, out var type))
            throw new ArgumentException($"Invalid {typeof(T).Name}");

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