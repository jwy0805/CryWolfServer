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