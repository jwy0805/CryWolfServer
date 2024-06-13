using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Bloom : Bud
{
    private bool _combo;
    private bool _critical;
    
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
        AttackImpactMoment = 0.4f;
        SkillImpactMoment = 0.25f;
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
        State = _combo ? State.Skill : State.Attack;
    }

    protected override void UpdateSkill()
    {
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment * 3 / 2);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed * 3 / 2);
        long impactMomentCorrection = LastAnimEndTime - timeNow + impactMoment;
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        AttackImpactEvents(impactTime);
        AttackImpactEvents(impactTime + impactMoment);
        AttackImpactEvents(impactTime + impactMoment * 2);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }

    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        int damage = TotalAttack;
        int rndInt = new Random().Next(100);
        if (_critical && rndInt < CriticalChance) damage = (int)(TotalAttack * CriticalMultiplier);
        target.OnDamaged(this, damage, Damage.Normal);
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
            State = _combo ? State.Skill : State.Attack;
            SetDirection();
        }
    }
}