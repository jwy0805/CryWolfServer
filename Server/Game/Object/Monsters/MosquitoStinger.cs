using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    private bool _woolStop = false;
    private bool _sheepDeath = false;
    private bool _infection = false;
    private readonly float _deathRate = 5;

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
        UnitRole = Role.Mage;
        SkillImpactMoment = 0.9f;
    }

    protected override void UpdateMoving()
    {
        if (Room == null) return;
        // Targeting
        Target = Room.FindClosestPriorityTarget(this, _typeList, Stat.AttackType); 
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);     
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance <= TotalAttackRange)
        {
            State = _sheepDeath && new Random().Next(100) < _deathRate ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId =  Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.MosquitoStingerProjectile, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        if (Target == null) return;
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            StingEffect(Target);
        });
    }

    private void StingEffect(GameObject? target)
    {
        if (target is not Sheep sheep) return;
        AttackEnded = true;
        Room?.Push(sheep.OnDamaged, this, 9999, Damage.True, false);
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (target is Creature)
        {
            if (new Random().Next(100) < FaintParam)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, target, this, 0, 1000, false);
            }

            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, 0, 5000, false);
        }

        if (target is Sheep sheep)
        {
            if (_infection) sheep.Infection = true;
            if (_woolStop) sheep.YieldStop = true;
            else sheep.YieldDecrement = Room.GameInfo.SheepYield * WoolDownRate / 100;
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            AttackEnded = true;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
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
        SyncPosAndDir();
    }
}