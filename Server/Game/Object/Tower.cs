using Google.Protobuf.Protocol;

namespace Server.Game;

public class Tower : GameObject
{
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }
}