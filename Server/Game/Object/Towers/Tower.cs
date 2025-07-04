using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Tower : Creature, ISkillObserver
{
    public Tower()
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
        Hp = MaxHp;

        if (State != State.Die) return;
        Target = null;
        Targetable = true;
        AlreadyRevived = false;
        WillRevive = false;
        AttackEnded = true;
        State = State.Idle;
        Room.Map.ApplyMap(this, new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ));
        Room.Broadcast(new S_State { ObjectId = Id, State = State.Idle });
        Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
        Room.SpawnEffect(EffectId.RegenerationEffect, this, this, PosInfo, true);
    }
    
    protected override void UpdateIdle()
    {   
        // Targeting
        Target = Room?.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Target.CellPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance <= TotalAttackRange)
        {
            State = State.Attack;
            SyncPosAndDir();
        }
    }

    protected override void OnDead(GameObject? attacker)
    {
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null) return;
        
        Targetable = false;
        State = State.Die;
        Room.RemoveAllBuffs(this);

        if (attacker != null)
        {
            attacker.KillLog = Id;
            attacker.Target = null;
            
            var monster = attacker as Monster ?? attacker.Parent as Monster;
            if (monster != null)
            {
                Room?.YieldDna(this, attacker);
            }
            
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile && attacker.Parent != null)
            {
                attacker.Parent.Target = null;
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (AttackEnded == false) AttackEnded = true;  
            
            Room?.Broadcast(new S_Die { ObjectId = Id, Revive = true });
            DieEvents(StdAnimTime * 2);
            return;
        }

        Room?.Broadcast(new S_Die { ObjectId = Id });
        Room?.DieTower(Id);
    }
}