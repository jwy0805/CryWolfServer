using Google.Protobuf.Protocol;

namespace Server.Game;

public class Monster : GameObject
{
    public int MonsterNo;
    
    public Monster()
    {
        ObjectType = GameObjectType.Monster;
    }
}