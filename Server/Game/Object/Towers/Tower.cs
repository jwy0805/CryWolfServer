using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public Vector3 RelativePosition => Room != null ? CellPos - Room.GameInfo.FenceStartPos : CellPos;

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

    public virtual void RoundInit()
    {
        if (Room == null) return;
        Target = null;
        Targetable = true;
        AlreadyRevived = false;
        WillRevive = false;
        AttackEnded = true;
        IsAttacking = false;
        State = State.Idle;
        Hp = MaxHp;
        Room.Map.ApplyMap(this, new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ));
        Room.Broadcast(new S_State { ObjectId = Id, State = State.Idle });
        Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
    }
    
    protected override void UpdateIdle()
    {   // Targeting
        Target = Room?.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance > TotalAttackRange) return;
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

        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.DieTower(Id);
    }
}