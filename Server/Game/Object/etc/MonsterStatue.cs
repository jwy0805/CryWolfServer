using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class MonsterStatue : GameObject
{
    public MonsterId MonsterId { get; set; }
    public int MonsterNum { get; set; }
    
    public MonsterStatue()
    {
        ObjectType = GameObjectType.MonsterStatue;
    }
    
    public override void Init()
    {
        DataManager.MonsterDict.TryGetValue(MonsterNum, out var monsterData);
        Stat.MergeFrom(monsterData!.stat);
        Hp = MaxHp;
    }
}