using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Tower : GameObject
{
    public int TowerNo;
    
    public Tower()
    {
        ObjectType = GameObjectType.Tower;
    }

    public void Init(int towerNo)
    {
        TowerNo = towerNo;

        DataManager.TowerDict.TryGetValue(TowerNo, out var towerData);
        Stat.MergeFrom(towerData!.stat);
        Stat.Hp = towerData.stat.MaxHp;

        State = State.Idle;
    }
}