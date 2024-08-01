using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Rabbit : Bunny
{
    private bool _aggro;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.RabbitAggro:
                    _aggro = true;
                    break;
                case Skill.RabbitDefence:
                    Defence += 3;
                    break;
                case Skill.RabbitEvasion:
                    Evasion += 10;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
        Player.SkillSubject.SkillUpgraded(Skill.RabbitAggro);
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && _aggro && Target != null)
            {
                State = State.Skill;
                return;
            }
        }
        
        switch (State)
        {
            case State.Die:
                UpdateDie();
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
            case State.Faint:
                break;
            case State.Standby:
                break;
        }   
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
        
        if (distance > AttackRange) return;
        State = _aggro && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void OnSkill()
    {
        if (Target == null || Target.Targetable == false) return;
        base.OnSkill();
        AttackEnded = false;
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.RabbitAggro, this, 5f);
            Mp = 0;
        });
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        if (pid == ProjectileId.RabbitAggro)
        {
            if (target is not Creature creature) return;
            Room.Push(AddBuffAction, BuffId.Aggro, BuffParamType.None, creature, this, 0, 2000, false);
        }
        else
        {
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        }
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            return;
        }
        
        State = _aggro && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}