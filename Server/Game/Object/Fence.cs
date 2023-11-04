using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Fence : GameObject
{
    public int FenceNum;

    public Fence()
    {
        ObjectType = GameObjectType.Fence;
    }

    public override void Init()
    {
        DataManager.FenceDict.TryGetValue(FenceNum, out var fenceData);
        if (fenceData == null) throw new InvalidDataException();
        Stat.MergeFrom(fenceData.stat);
        Stat.Hp = fenceData.stat.MaxHp;
    }
}