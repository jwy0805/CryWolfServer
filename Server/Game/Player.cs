using Google.Protobuf.Protocol;

namespace Server.Game;

public class Player
{
    public ObjectInfo Info { get; set; } = new();
    public GameRoom Room { get; set; }
    public ClientSession Session { get; set; }
}