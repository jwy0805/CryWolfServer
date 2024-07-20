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

    public GameRoom CreateGameRoom(int mapId, bool test = false)
    {
        GameRoom gameRoom = new GameRoom();
        if (test) gameRoom.RoomActivated = true;
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
}