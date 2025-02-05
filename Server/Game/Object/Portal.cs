using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Portal : GameObject      
{
    public int Level { get; private set; }
    
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

    public void LevelUp()
    {
        if (Room == null) return;
        
        Level++;
        if (Level > 3)
        {
            Level = 3;
            return;
        }

        Room.GameInfo.NorthMaxTower += 4;
        
        if (Level == 2)
        {
            Room.GameInfo.WolfYield += 20;
        }
        else if (Level == 3)
        {
            Room.GameInfo.WolfYield += 40;
        }
        
        var packet = new S_BaseUpgrade
        {
            Faction = Faction.Wolf,
            Level = Level,
        };
        
        Room.Broadcast(packet);
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        if (Room == null) return;
        
        Targetable = false;
        
        if (attacker != null)
        {
            attacker.KillLog = Id;
            attacker.Target = null;
            
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile && attacker.Parent != null)
            {
                attacker.Parent.Target = null;
            }
        }
        
        Room.Broadcast(new S_Die { ObjectId = Id});
        Room.DieAndLeave(Id);
    }
}