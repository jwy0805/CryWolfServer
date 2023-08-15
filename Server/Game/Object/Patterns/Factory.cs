using Google.Protobuf.Protocol;

namespace Server.Game;

public interface IFactory
{
    public int GenerateId(GameObjectType type);
}

public interface IGameObject
{
    public void Init();
}