namespace Server.Game;

public class RoomManager
{
    public static RoomManager Instance { get; } = new();

    private readonly object _lock = new();
    private Dictionary<int, GameRoom> _rooms = new();
    private int _roomId = 1;
    
    public GameRoom Add(int mapId)
    {
        GameRoom gameRoom = new();
        gameRoom.Init(mapId);

        lock (_lock)
        {
            gameRoom.RoomId = _roomId;
            _rooms.Add(_roomId, gameRoom);
            _roomId++;
        }

        return gameRoom;
    }

    public bool Remove(int roomId)
    {
        lock (_lock)
        {
            return _rooms.Remove(roomId);
        }
    }

    public GameRoom? Find(int roomId)
    {
        lock (_lock)
        {
            return _rooms.TryGetValue(roomId, out var room) ? room : null;
        }
    } 
}
