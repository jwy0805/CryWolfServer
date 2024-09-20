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
        DataManager.ObjectDict.TryGetValue(602, out var objectData);
        Stat.MergeFrom(objectData!.stat);
        Hp = MaxHp;
        Targetable = false;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        Targetable = Room.IsThereAnyMonster();
    }

    protected override void OnDead(GameObject? attacker)
    {
        if (Room == null) return;
        
        Targetable = false;
        if (attacker != null)
        {
            attacker.KillLog = Id;
            if (attacker.Target != null)
            {
                if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
                {
                    if (attacker.Parent != null) attacker.Parent.Target = null;
                }
                attacker.Target = null;
            }
        }
        
        Room.Broadcast(new S_Die { ObjectId = Id});
        Room.DieAndLeave(Id);
    }
}