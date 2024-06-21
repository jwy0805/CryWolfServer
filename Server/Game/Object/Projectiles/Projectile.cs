using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Projectile : GameObject
{
    protected readonly Scheduler Scheduler = new();   
    public ProjectileId ProjectileId { get; set; }
    
    protected Projectile()
    {
        ObjectType = GameObjectType.Projectile;
    }
    
    public override void Init()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            Room.Push(Room.LeaveGame, Id);
            return;
        }
        
        CalculateAttackTime();
    }

    private void CalculateAttackTime()
    {
        float distance = Vector3.Distance(DestPos, CellPos);
        long attackTime = (long)(distance / MoveSpeed * 1000);
        AttackImpact(attackTime);
    }

    protected virtual async void AttackImpact(long impactTime)
    {
        if (Parent == null || Target == null || Target.Targetable == false || Room == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null) return;
            if (Parent is Creature creature) creature.ApplyProjectileEffect(Target, ProjectileId);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}