using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.etc;

public class TowerStatue : GameObject
{
    public TowerId TowerId { get; set; }
    public int TowerNum { get; set; }
    
    public TowerStatue()
    {
        ObjectType = GameObjectType.TowerStatue;
    }
    
    public override void Init()
    {
        DataManager.TowerDict.TryGetValue(TowerNum, out var towerData);
        Stat.MergeFrom(towerData!.stat);
        Hp = MaxHp;
    }

    public override void Update()
    {
        base.Update();
        Console.WriteLine($"{PosInfo.PosX}, {PosInfo.PosZ}");
    }
}