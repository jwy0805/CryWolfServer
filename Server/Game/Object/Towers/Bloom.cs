using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Bloom : Bud
{
    private bool _combo;
    private bool _critical;
    private Guid _attackTaskId2;
    private Guid _attackTaskId3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.Bloom3Combo:
                    _combo = true;
                    Attack -= 10;
                    AttackSpeed = 1.0f;
                    break;
                case Skill.BloomCritical:
                    _critical = true;
                    CriticalChance += 20;
                    break;
                case Skill.BloomCriticalDamage:
                    CriticalMultiplier += 0.25f;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
        AttackImpactMoment = 0.4f;
        SkillImpactMoment = 0.25f;
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
            State = _combo ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
    }

    protected override void UpdateSkill()
    {
        if (Room == null) return;
        
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            if (AttackEnded) return;
            AttackEnded = true;
            Scheduler.CancelEvent(AttackTaskId);
            Scheduler.CancelEvent(_attackTaskId2);
            Scheduler.CancelEvent(_attackTaskId3);
            Scheduler.CancelEvent(EndTaskId);
            SetNextState();
        }
    }

    protected override void OnSkill()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Hp <= 0) return;
        
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment * 3 / 2);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed * 3 / 2);
        SkillImpactEvents(impactMoment);
        SkillImpactEvents2(impactMoment + impactMoment);
        SkillImpactEvents3(impactMoment + impactMoment * 2);
        EndEvents(animPlayTime); 
        AttackEnded = false;
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });            
    }

    private void SkillImpactEvents2(long impactTime)
    {
        _attackTaskId2 =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });     
    }
    
    private void SkillImpactEvents3(long impactTime)
    {
        _attackTaskId3 =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.SeedProjectile, this, 5f);
        });     
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        int damage = TotalAttack;
        int rndInt = new Random().Next(100);
        if (_critical && rndInt < CriticalChance) damage = (int)(TotalAttack * CriticalMultiplier);
        Room?.Push(target.OnDamaged, this, damage, Damage.Normal, false);
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            return;
        }

        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
        }
        else
        {
            State = _combo ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
    }

    protected override void OnDead(GameObject? attacker)
    {
        Scheduler.CancelEvent(_attackTaskId2);
        Scheduler.CancelEvent(_attackTaskId3);
        base.OnDead(attacker);
    }
}