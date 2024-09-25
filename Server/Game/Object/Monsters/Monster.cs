using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Monster : Creature, ISkillObserver
{
    public int StatueId { get; set; }
    
    public float DnaYieldParam { get; set; }
    public int DnaYield
    {
        get
        {
            if (Room == null) return 0;
            return (int)(Room.GameInfo.WolfYield + DnaYieldParam);
        }
    }

    protected Monster()
    {
        ObjectType = GameObjectType.Monster;
    }

    public override void Init()
    {
        base.Init();
        Player.SkillSubject.AddObserver(this);
        DataManager.UnitDict.TryGetValue((int)UnitId, out var unitData);
        Stat.MergeFrom(unitData?.stat);
        
        StatInit();
        SkillInit();
    }

    protected override void UpdateIdle()
    {
        Target = Room?.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        State = State.Moving;
    }
    
    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null) return;
        
        Targetable = false;
        State = State.Die;
        
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
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (AttackEnded == false) AttackEnded = true;  
            
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true });
            DieEvents(StdAnimTime * 2);
            return;
        }

        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.DieAndLeave(Id);        
    }

    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null) return;

        base.ApplyAttackEffect(target);
        Room.GameInfo.WolfResource += Room.GameInfo.WolfYield;
    }
}