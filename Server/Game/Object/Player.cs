using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Player : GameObject
{
    public int PlayerNo;
    
    public ClientSession Session { get; set; }

    public Player()
    {
        ObjectType = GameObjectType.Player;
        
        DataManager.PlayerDict.TryGetValue(PlayerNo, out var playerData);
        Stat.MergeFrom(playerData!.stat);
        Stat.MoveSpeed = playerData.stat.MoveSpeed;
    }
}