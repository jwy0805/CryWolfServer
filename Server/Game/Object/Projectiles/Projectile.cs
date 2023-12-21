using Google.Protobuf.Protocol;

namespace Server.Game;

public class Projectile : GameObject
{
    public ProjectileId ProjectileId { get; set; }
    
    public override void Init()
    {
        GameRoom? room = Room;
        MoveSpeed = 4f;
        if (room == null) return;
        if (Target == null || Target.Stat.Targetable == false) room.Push(room.LeaveGame, Id);
        DestPos = Target!.CellPos;
        // Dir = (float)Math.Atan2(Target.CellPos.X - CellPos.X, Target.CellPos.Z - CellPos.Z);
        // BroadcastDest();
    }

    protected Projectile()
    {
        ObjectType = GameObjectType.Projectile;
    }

    public override void BroadcastDest()
    {
        S_SetDest destPacket = new S_SetDest { ObjectId = Id, MoveSpeed = MoveSpeed };
        DestVector destVector = new DestVector { X = DestPos.X, Y = DestPos.Y + Target!.Stat.SizeY, Z = DestPos.Z };
        destPacket.Dest.Add(destVector);
        Room?.Broadcast(destPacket);
    }

    public virtual void SetProjectileEffect(GameObject master) { }
}