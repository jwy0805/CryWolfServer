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

    public void Init()
    {
        DataManager.FenceDict.TryGetValue(FenceNum, out var fenceData);
        Stat.MergeFrom(fenceData!.stat);
        Stat.Hp = fenceData.stat.MaxHp;
    }
}