using Google.Protobuf.Protocol;

namespace Server.Game;

public class ObjectManager
{
    public static ObjectManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<int, Player> _players = new();

    // [UNUSED(1)][TYPE(7)][ID(24)]
    private int _counter = 0;
    
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