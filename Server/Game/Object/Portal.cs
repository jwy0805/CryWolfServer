using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Portal : GameObject      
{
    public Portal()
    {
        ObjectType = GameObjectType.Portal;
    }

    public override void Init()
    {
        DataManager.ObjectDict.TryGetValue(2, out var objectData);
        if (objectData == null) throw new InvalidDataException();
        Stat.MergeFrom(objectData.stat);
    }
}