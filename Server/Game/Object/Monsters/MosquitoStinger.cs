using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    private bool _woolStop = false;
    private bool _sheepDeath = false;
    private bool _infection = false;
    private readonly float _deathRate = 50;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoStingerWoolStop:
                    _woolStop = true;
                    break;
                case Skill.MosquitoStingerInfection:
                    _infection = true;
                    break;
                case Skill.MosquitoStingerSheepDeath:
                    _sheepDeath = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        SkillImpactMoment = 0.9f;
        Player.SkillSubject.SkillUpgraded(Skill.MosquitoStingerSheepDeath);
    }

    protected override void UpdateSkill()
    {
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long impactMomentCorrection = LastAnimEndTime - timeNow + impactMoment;
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        StingEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.MosquitoStingerProjectile, this, 5f);
        });
    }
    
    private async void StingEvents(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            ApplyAttackEffect(Target);
        });
    }

    public override void ApplyAttackEffect(GameObject? target)
    {
        if (target is not Sheep sheep) return;
        sheep.OnDamaged(this, 9999, Damage.True);
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        
        if (target is Creature _)
        {
            BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1000);
            BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0, 5000);
        }
        
        if (target is not Sheep sheep) return;
        if (_sheepDeath && new Random().Next(100) < _deathRate)
        {
            sheep.OnDamaged(this, 9999, Damage.True);
            return;
        }
        
        if (_infection) sheep.Infection = true;
        if (_woolStop) sheep.YieldStop = true;
        else sheep.YieldDecrement = sheep.Resource * WoolDownRate / 100;
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
            return;
        }

        var randomInt = new Random().Next(100);
        if (_sheepDeath && randomInt > _deathRate && Target is Sheep _) State = State.Skill;
        else State = State.Attack;
        SetDirection();
    }
}