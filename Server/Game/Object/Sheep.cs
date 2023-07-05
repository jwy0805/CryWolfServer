using Google.Protobuf.Protocol;

namespace Server.Game;

public class Sheep : GameObject
{
    public int SheepNo;

    public Sheep()
    {
        ObjectType = GameObjectType.Sheep;
    }
}