using Google.Protobuf.Protocol;
using Server.Game.Resources;

namespace Server.Game;

public sealed partial class ObjectManager : IFactory
{
    public static ObjectManager Instance { get; } = new();

    private readonly object _lock = new();
    
    // [UNUSED(1)][TYPE(7)][ID(24)]
    private int _counter = 0;

    public T Create<T>(Enum id) where T : GameObject
    {
        IFactory<T>? factory = id switch
        {
            UnitId towerId when typeof(T) == typeof(Tower) => _towerDict[towerId] as IFactory<T>,
            UnitId monsterId when typeof(T) == typeof(Monster) => _monsterDict[monsterId] as IFactory<T>,
            SheepId sheepId when typeof(T) == typeof(Sheep) => _sheepDict[sheepId] as IFactory<T>,
            ProjectileId projectileId when typeof(T) == typeof(Projectile) =>
                _projectileDict[projectileId] as IFactory<T>,
            EffectId effectId when typeof(T) == typeof(Effect) => _effectDict[effectId] as IFactory<T>,
            ResourceId resourceId when typeof(T) == typeof(Resource) => _resourceDict[resourceId] as IFactory<T>,
            _ => throw new InvalidDataException()
        };
        
        if (factory == null) throw new InvalidDataException();
        var gameObject = factory.Create();
        lock (_lock) gameObject.Id = GenerateId(gameObject.ObjectType);

        return gameObject;
    }
    
    public T Add<T>() where T : GameObject, new()
    {
        T gameObject = new T();

        lock (_lock)
        {
            gameObject.Id = GenerateId(gameObject.ObjectType);
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
}