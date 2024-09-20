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
        DataManager.ObjectDict.TryGetValue(601, out var objectData);
        if (objectData == null) return;
        Stat.MergeFrom(objectData.stat);
    }
}