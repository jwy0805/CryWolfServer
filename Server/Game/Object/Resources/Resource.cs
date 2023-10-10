using Google.Protobuf.Protocol;

namespace Server.Game.Resources;

public class Resource : GameObject
{
    public int ResourceNum;
    public ResourceId ResourceId;

    public void Init()
    {
        ResourceNum = (int)ResourceId;
    }
}