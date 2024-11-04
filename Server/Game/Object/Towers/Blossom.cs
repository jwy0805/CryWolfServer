using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Blossom : Bloom
{
    private bool _blossomDeath = false;
    private bool _faintCritical = false;
    private bool _powerAttack = false;
    private int _attackRemainder = 0;
    private readonly int _deathProb = 4;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BlossomAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.3f;
                    break;
                case Skill.BlossomDeath:
                    _blossomDeath = true;
                    break;
                case Skill.BlossomFaintCritical:
                    _faintCritical = true;
                    break;
                case Skill.BlossomPowerAttack:
                    _powerAttack = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
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
        
        if (distance > TotalAttackRange) return;
        State =  State.Attack;
        SyncPosAndDir();
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
            State = State.Attack;
            SyncPosAndDir();
        }
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;

            if (_blossomDeath == false)
            {
                Room.SpawnProjectile(ProjectileId.BlossomProjectile, this, 5f);
            }
            else
            {
                int rndInt = new Random().Next(100);
                Room.SpawnProjectile(
                    rndInt < _deathProb ? ProjectileId.BlossomDeathProjectile : ProjectileId.BlossomProjectile,
                    this, 5f);
            }
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null) return;
        
        if (pid == ProjectileId.BlossomProjectile)
        {
            if (_faintCritical)
            {
                if (target.State == State.Faint)
                {
                    var criticalRate = CriticalChance;
                    CriticalChance = 100;
                    Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
                    CriticalChance = criticalRate;
                    return;
                }
            }

            if (_powerAttack)
            {
                Room.Push(target.OnDamaged, this, TotalAttack + _attackRemainder, Damage.Normal, false);
                _attackRemainder = 0;
            }
            else
            {
                Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
            }
        }       
        else
        {
            if (_powerAttack)
            {
                _attackRemainder = target.Hp / 2;
            }
            
            Room.Push(target.OnDamaged, this, 9999, Damage.True, false);
        }
    }
}