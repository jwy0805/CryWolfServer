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
        DataManager.ObjectDict.TryGetValue(3, out var objectData);
        Stat.MergeFrom(objectData!.stat);
        Hp = MaxHp;
    }
}