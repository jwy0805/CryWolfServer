using Google.Protobuf.Protocol;

namespace Server.Game;

public class Tower : GameObject
{
    public int TowerNo;
    
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }
}