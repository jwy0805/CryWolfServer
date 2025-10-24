namespace Server.Game;

public class GameLogic : JobSerializer
{
    public static GameLogic Instance { get; } = new();

    private readonly Dictionary<int, GameRoom> _rooms = new();
    private int _roomId = 1;
    private readonly object _lock = new();

    public void Update()
    { 
        Flush();

        foreach (var room in _rooms.Values)
        {
            room.Update();
        }
    }

    public GameRoom CreateGameRoom(int mapId)
    {
        var room = new GameRoom();
        room.Push(room.Init, mapId);
        room.RoomId = _roomId;
        _rooms.Add(_roomId, room);
        Console.WriteLine($"Game room created: {room.RoomId} for map {mapId}");
        _roomId++;

        return room;
    }
    
    public void RemoveGameRoom(int roomId)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var room) == false) return;

            try
            {
                room.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            _rooms.Remove(room.RoomId);
        }
    }

    public GameRoom? Find(int roomId)
    {
        return _rooms.GetValueOrDefault(roomId);
    }
    
    public GameRoom? FindByUserId(int userId)
    {
        return _rooms.Values.FirstOrDefault(room => 
            room.FindPlayer(go => go is Player player && player.Session?.UserId == userId ) != null);
    }
}