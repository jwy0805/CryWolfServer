using Google.Protobuf.Protocol;

namespace Server.Game;

public class Projectile : GameObject
{
    public virtual void Init()
    {
        GameRoom? room = Room;
        MoveSpeed = 18f;
        if (room == null) return;
        if (Target == null || Target.Stat.Targetable == false) room.Push(room.LeaveGame, Id);
        DestPos = Target!.CellPos;
        BroadcastDest();
    }
    
    public Projectile()
    {
        ObjectType = GameObjectType.Projectile;
    }

    public override void Update()
    {
        
    }

    public override void BroadcastDest()
    {
        S_SetDest destPacket = new S_SetDest { ObjectId = Id };
        DestVector destVector = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z };
        destPacket.Dest.Add(destVector);
        Room?.Broadcast(destPacket);
    }
}