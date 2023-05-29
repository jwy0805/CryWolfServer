namespace Server.Game;

public class RoomManager
{
    public static RoomManager Instance { get; } = new RoomManager();

    private readonly object _lock = new();
    private Dictionary<int, GameRoom> _rooms = new();
    private int _roomId = 1;
    
    public GameRoom Add()
    {
        GameRoom gameRoom = new();

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
