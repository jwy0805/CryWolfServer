using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SkeletonMage : SkeletonGiant
{
    private bool _adjacentRevive = false;
    private bool _killRecoverMp = false;
    private bool _reviveHealthUp = false;
    private bool _curse = false;
    private int _killLog = 0;
    
    public override int KillLog
    {
        get => _killLog;
        set
        {
            _killLog = value;
            if (_killRecoverMp == false) return;
            Mp += 20;
            if (Mp > MaxMp) Mp = MaxMp;
        }
    }

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonMageAdjacentRevive:
                    _adjacentRevive = true;
                    break;
                case Skill.SkeletonMageKillRecoverMp:
                    _killRecoverMp = true;
                    break;
                case Skill.SkeletonMageReviveHealthUp:
                    _reviveHealthUp = true;
                    ReviveHpRate = 0.6f;
                    break;
                case Skill.SkeletonMageCurse:
                    _curse = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Supporter;
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonMageAdjacentRevive);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonMageKillRecoverMp);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonMageReviveHealthUp);
        Player.SkillSubject.SkillUpgraded(Skill.SkeletonMageCurse);
    }

    public override void Update()
    {
        base.Update();
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
            State = Mp >= MaxMp ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SkeletonMageProjectile, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Mp = 0;
            Room.SpawnEffect(EffectId.SkeletonGiantSkill, this, PosInfo);
            
            // AttackSteal
            foreach (var target in DebuffTargets)
            {
                target.AttackParam -= AttackStealParam;
                AttackParam += AttackStealParam;
            }
            
            // AdjacentRevive
            if (_adjacentRevive)
            {
                var reviveTargets = Room.FindTargets(this,
                    new[] { GameObjectType.Monster }, TotalAttackRange);
                if (reviveTargets.Any())
                {
                    foreach (var creature in reviveTargets
                                 .Where(gameObject => gameObject is { WillRevive: false, AlreadyRevived: false })
                                 .Where(gameObject => gameObject.Id != Id)
                                 .OrderBy(_ => Guid.NewGuid()).Take(1))
                    {
                        Room.SpawnEffect(EffectId.WillRevive, creature, creature.PosInfo, true, 1000000);
                        creature.WillRevive = true;
                        if (_reviveHealthUp) creature.ReviveHpRate = 0.6f;
                    }
                }
            }
            
            // Curse
            if (_curse)
            {
                var curseTargets = Room.FindTargets(
                    this, new[] { GameObjectType.Tower }, TotalSkillRange).ToList();
                if (curseTargets.Any())
                {
                    foreach (var creature in curseTargets
                                 .Where(gameObject => gameObject is { Hp: > 1, Targetable: true })
                                 .OrderBy(_ => Guid.NewGuid()).Take(1))
                    {
                        BuffManager.Instance.AddBuff(BuffId.Curse, creature, this, 0, 3000);
                    }
                }
            }
        });
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        var targetPos = new Vector3(target.CellPos.X, target.CellPos.Y, target.CellPos.Z);
        
        if (PreviousTargetId != target.Id)
        {
            AdditionalAttackParam = 0;
            PreviousTargetId = target.Id;
        }
        
        var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var targets = Room.FindTargets(targetPos, types, DebuffRange);
        foreach (var t in targets) t.DefenceParam -= DefenceDebuffParam;
        DebuffTargets = targets;

        if (target.TotalDefence <= 0) AdditionalAttackParam += DefenceDownParam;
        target.OnDamaged(this, TotalAttack + AdditionalAttackParam, Damage.Magical);
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
            State = Mp >= MaxMp ? State.Skill : State.Attack;
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
        
        if (AlreadyRevived == false || WillRevive)
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