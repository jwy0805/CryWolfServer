using Google.Protobuf.Protocol;

namespace Server.Game;

public class Fence : GameObject
{
    public int FenceNo;

    public Fence()
    {
        ObjectType = GameObjectType.Fence;
    }
}