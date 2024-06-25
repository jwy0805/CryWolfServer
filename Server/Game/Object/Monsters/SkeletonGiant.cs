using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SkeletonGiant : Skeleton
{
    private bool _defenceDebuff = false;
    private bool _attackSteal = false;
    private bool _reviveSelf = false;
    protected List<GameObject> DebuffTargets = new();
    
    protected readonly int DefenceDebuffParam = 3;
    protected float DebuffRange = 2f;
    protected readonly int AttackStealParam = 2;
    protected readonly int ReviveAnimTime = 1000;
    protected readonly int DeathStandbyTime = 2000;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonGiantDefenceDebuff:
                    _defenceDebuff = true;
                    break;
                case Skill.SkeletonGiantAttackSteal:
                    _attackSteal = true;
                    break;
                case Skill.SkeletonGiantMpDown:
                    MaxMp -= 25;
                    break;
                case Skill.SkeletonGiantRevive:
                    _reviveSelf = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }
        
        switch (State)
        {
            case State.Die:
                UpdateDie();
                break;
            case State.Moving:
                UpdateMoving();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Skill:
                UpdateSkill();
                break;
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Revive:
            case State.Faint:
            case State.Standby:
                break;
        }
    }
    
    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
       
        if (distance <= TotalAttackRange)
        {
            State = _defenceDebuff && Mp >= MaxMp ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Mp = 0;
            Room.SpawnEffect(EffectId.SkeletonGiantSkill, this, PosInfo);
            
            foreach (var target in DebuffTargets)
            {
                target.AttackParam -= AttackStealParam;
                AttackParam += AttackStealParam;
            }
        });
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        var targetPos = new Vector3(target.CellPos.X, target.CellPos.Y, target.CellPos.Z);
        var effectPos = new PositionInfo { PosX = targetPos.X, PosY = targetPos.Y + 0.5f, PosZ = targetPos.Z };
        Room.SpawnEffect(EffectId.SkeletonGiantEffect, target, effectPos, true);
        
        if (PreviousTargetId != target.Id)
        {
            AdditionalAttackParam = 0;
            PreviousTargetId = target.Id;
        }
        
        if (_defenceDebuff)
        {
            var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
            var targets = Room.FindTargets(targetPos, types, DebuffRange);
            foreach (var t in targets) t.DefenceParam -= DefenceDebuffParam;
            DebuffTargets = targets;
        }
        else
        {
            target.DefenceParam -= DefenceDownParam;
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        if (target.Hp <= 0) return;
        if (target.TotalDefence <= 0) AdditionalAttackParam += DefenceDownParam;
        if (AdditionalAttackParam > 0)
        {
            target.OnDamaged(this, AdditionalAttackParam, Damage.Magical);
        }
    }

    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);  

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            AttackEnded = true;
        }
        else
        {
            State = _defenceDebuff && Mp >= MaxMp ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
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

        if (AlreadyRevived == false && (_reviveSelf || WillRevive))
        {
            if (IsAttacking) IsAttacking = false;
            if (AttackEnded == false) AttackEnded = true;  
            
            State = State.Die;
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true });
            DieEvents(DeathStandbyTime);
            return;
        }

        S_Die diePacket = new() { ObjectId = Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }
}