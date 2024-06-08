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
        if (Target == null || Target.Stat.Targetable == false)
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
        AttackImpactTime(attackTime);
    }

    protected virtual async void AttackImpactTime(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Parent is Creature creature) creature.ApplyProjectileEffect(Target);
            Room?.Push(Room.LeaveGameOnlyServer, Id);
        });
    }
}