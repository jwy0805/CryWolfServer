using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class MonsterStatue : GameObject
{
    public UnitId UnitId { get; set; }
    
    public MonsterStatue()
    {
        ObjectType = GameObjectType.MonsterStatue;
    }
    
    public override void Init()
    {
        DataManager.UnitDict.TryGetValue((int)UnitId, out var unitData);
        Stat.MergeFrom(unitData!.stat);
        Hp = MaxHp;
    }
}