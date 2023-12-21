using Google.Protobuf.Protocol;
using Server.Game.Resources;

namespace Server.Game;

public sealed partial class ObjectManager : IFactory
{
    public static ObjectManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<int, Player?> _players = new();
    
    // [UNUSED(1)][TYPE(7)][ID(24)]
    private int _counter = 0;
    
    public Tower CreateTower(TowerId towerId)
    {
        if (!_towerDict.TryGetValue(towerId, out var factory)) throw new InvalidDataException();
        GameObject gameObject = factory.CreateTower();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as Tower ?? throw new InvalidCastException();
    }

    public Monster CreateMonster(MonsterId monsterId)
    {
        if (!_monsterDict.TryGetValue(monsterId, out var factory)) throw new InvalidDataException();
        GameObject gameObject = factory.CreateMonster();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as Monster ?? throw new InvalidCastException();
    }

    public MonsterStatue CreateMonsterStatue()
    {
        StatueFactory factory = new();
        GameObject gameObject = factory.CreateStatue();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as MonsterStatue ?? throw new InvalidCastException();
    }
    
    public Projectile CreateProjectile(ProjectileId projectileId)
    {
        if (!_projectileDict.TryGetValue(projectileId, out var factory)) throw new InvalidDataException();
        GameObject gameObject = factory.CreateProjectile();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as Projectile ?? throw new InvalidCastException();
    }

    public Effect CreateEffect(EffectId effectId)
    {
        if (!_effectDict.TryGetValue(effectId, out var factory)) throw new InvalidDataException();
        GameObject gameObject = factory.CreateEffect();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as Effect ?? throw new InvalidCastException();
    }

    public Resource CreateResource(ResourceId resourceId)
    {
        if (!_resourceDict.TryGetValue(resourceId, out var factory)) throw new InvalidDataException();
        GameObject gameObject = factory.CreateResource();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);
        
        return gameObject as Resource ?? throw new InvalidCastException();
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