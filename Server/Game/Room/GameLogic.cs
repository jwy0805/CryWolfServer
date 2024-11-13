namespace Server.Game;

public class GameLogic : JobSerializer
{
    public static GameLogic Instance { get; } = new();

    private Dictionary<int, GameRoom> _rooms = new();
    private int _roomId = 1;

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
        GameRoom gameRoom = new GameRoom();
        
        gameRoom.Push(gameRoom.Init, mapId);
        gameRoom.RoomId = _roomId;
        _rooms.Add(_roomId, gameRoom);
        _roomId++;

        return gameRoom;
    }

    public bool Remove(int roomId)
    {
        return _rooms.Remove(roomId);
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