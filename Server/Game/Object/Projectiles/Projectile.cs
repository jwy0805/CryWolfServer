using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Projectile : GameObject
{
    protected bool FirstUpdated = false; // Update 1회 실행 이후 Spawn Packet이 전송되기 때문에 최초 1회 아무것도 안하고 return
    protected List<Vector3> FullPath = new();
    public bool ClientResponse { get; set; } = false;
    public ProjectileId ProjectileId { get; set; }
    
    protected Projectile()
    {
        ObjectType = GameObjectType.Projectile;
    }
    
    public override void Init()
    {
        MoveSpeed = 5f;
        if (Room == null) return;
        if (Target == null || Target.Stat.Targetable == false)
        {
            Room.Push(Room.LeaveGame, Id);
            return;
        }
        DestPos = Target.CellPos;
        FullPath = Room.Map.GetProjectilePath(this);
        var xDiff = DestPos.X - CellPos.X;
        var zDiff = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(xDiff, zDiff) * (180 / Math.PI), 2);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (FirstUpdated == false)
        {
            FirstUpdated = true;
            return;
        }
        
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos));
        if (distance < 0.25)
        {
            if (Parent is Creature parent) parent.ApplyProjectileEffect(Target);
            Room.Push(Room.LeaveGameOnlyServer, Id);
            return;
        }
        
        Path = Room.Map.ProjectileMove(this);
        if (ClientResponse == false) return;
        BroadcastProjectilePath();
        ClientResponse = false;
    }
}