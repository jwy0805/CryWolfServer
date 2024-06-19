using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Blossom : Bloom
{
    private bool _blossomDeath = false;
    private readonly int _deathProb = 3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.BlossomAttack:
                    Attack += 10;
                    break;
                case Skill.BlossomDeath:
                    _blossomDeath = true;
                    break;
            }
        }
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
        
        if (distance > TotalAttackRange) return;
        State =  State.Attack;
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
            State = State.Attack;
            SyncPosAndDir();
        }
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
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
        if (pid == ProjectileId.BlossomProjectile)
        {
            target.OnDamaged(this, TotalAttack, Damage.Normal);
        }       
        else
        {
            target.OnDamaged(this, 9999, Damage.True);
        }
    }
}