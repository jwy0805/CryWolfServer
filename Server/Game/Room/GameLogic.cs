using System.Collections.Concurrent;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class GameLogic : JobSerializer
{
    public static GameLogic Instance { get; } = new();

    private readonly Dictionary<int, GameRoom> _rooms = new();
    
    private int _roomId = 1;

    private readonly RoomActorScheduler _scheduler = new (Math.Max(1, Environment.ProcessorCount - 1));
    
    public void Update()
    { 
        Flush();

        // 직접 실행하지 않고 워커풀에 스케쥴링만 함
        foreach (var room in _rooms.Values)
        {
            if (room.IsShuttingDown) continue;
            
            Interlocked.Exchange(ref room._tickPending, 1);
            _scheduler.Schedule(room);
        }
    }

    // Must be called only from GameLogic's serialized context (inside Push/Flush).
    public GameRoom CreateGameRoom(int mapId, GameMode mode)
    {
        var room = new GameRoom();
        room.Init(_roomId, mapId, mode);
        _rooms.Add(_roomId++, room);
        Metrics.IncreaseRoom();
        Console.WriteLine($"Game room created: {room.RoomId} for map {mapId}");
    
        return room;
    }
    
    // Must be called only from GameLogic's serialized context (inside Push/Flush).
    public void RemoveGameRoom(int roomId)
    {
        if (_rooms.Remove(roomId))
        {
            Metrics.DecreaseRoom();
            Console.WriteLine($"Game room removed: {roomId}");
        }
    }

    // Must be called only from GameLogic's serialized context (inside Push/Flush).
    public GameRoom? Find(int roomId)
    {
        return _rooms.GetValueOrDefault(roomId);
    }

    // Must be called only from GameLogic's serialized context (inside Push/Flush).
    public GameRoom? FindByUserId(int userId)
    {
        return _rooms.Values.FirstOrDefault(room =>
            room.FindPlayer(go => go is Player player && player.Session?.UserId == userId) != null);
    }
}