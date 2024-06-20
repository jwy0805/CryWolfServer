using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{ 
    public Vector3 StartCell { get; set; }

    protected Tower()
    {
        ObjectType = GameObjectType.Tower;
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
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance > AttackRange) return;
        State = State.Attack;
        SyncPosAndDir();
    }

    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
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
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (IsAttacking) IsAttacking = false;
            if (AttackEnded == false) AttackEnded = true;  
            
            State = State.Die;
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true });
            DieEvents(StdAnimTime * 2);
            return;
        }

        S_Die diePacket = new() { ObjectId = Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);            }
}