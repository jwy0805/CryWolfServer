using Server.Util;

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
        Metrics.IncreaseRoom();
        Console.WriteLine($"Game room created: {room.RoomId} for map {mapId}");
        _roomId++;
    
        return room;
    }

    public Task<GameRoom> CreateGameRoomAsync(int mapId)
    {
        var tcs = new TaskCompletionSource<GameRoom>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        Push(() =>
        {
            var room = new GameRoom();
            room.Push(room.Init, mapId);
            room.RoomId = _roomId++;
            _rooms.Add(room.RoomId, room);
            Console.WriteLine($"Game room created: {room.RoomId} for map {mapId}");
            tcs.SetResult(room);
        });

        return tcs.Task;
    }
    
    public void RemoveGameRoom(int roomId)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var room) == false) return;

            try
            {
                room.Dispose();
                Metrics.DecreaseRoom();
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