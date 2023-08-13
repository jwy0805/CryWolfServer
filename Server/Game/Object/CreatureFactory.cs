using Google.Protobuf.Protocol;

namespace Server.Game;

public class CreatureFactory
{
    private static readonly Dictionary<TowerId, Type?> _towerDict = new Dictionary<TowerId, Type?>
    {
        
    };

    private static readonly Dictionary<MonsterId, Type?> _monsterDict = new Dictionary<MonsterId, Type?>
    {
        { MonsterId.Snakelet, typeof(Snakelet) },
        { MonsterId.Snake, typeof(Snake) },
        { MonsterId.SnakeNaga, typeof(SnakeNaga) }
    };

    public static Tower CreateTower(TowerId towerId)
    {
        if (_towerDict.TryGetValue(towerId, out var type))
            return (Tower)Activator.CreateInstance(type);
        throw new ArgumentException("Invalid TowerId");
    }

    public static Monster CreateMonster(MonsterId monsterId)
    {
        if (_monsterDict.TryGetValue(monsterId, out var type))
            return (Monster)Activator.CreateInstance(type);
        throw new ArgumentException("Invalid MonsterId");
    }
}