using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunBlossom : Tower
{
    private bool _heal;
    private bool _defenceBuff;
    
    protected int HealParam = 50;
    protected int DefenceBuffParam = 5;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunBlossomHeal:
                    _heal = true;
                    break;
                case Skill.SunBlossomSelfDefence:
                    Defence += 3;
                    break;
                case Skill.SunBlossomDefence:
                    _defenceBuff = true;
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
            if (Mp >= MaxMp && _heal)
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
            case State.Revive:
            case State.Faint:
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

        if (distance > TotalAttackRange) return;
        State = _heal && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.BasicProjectile4, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            
            var types = new[] { GameObjectType.Tower };
            
            // Heal
            if (_heal)
            {
                var target = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                    .MinBy(target => target.Hp / target.MaxHp);
                if (target != null)
                {
                    Room.Push(AddBuffAction, BuffId.HealBuff,
                        BuffParamType.Constant, target, this, HealParam, 1000, false);
                }
            }
            
            // Defence Buff
            if (_defenceBuff)
            {
                var target = Room.FindTargets(this, types, TotalSkillRange, AttackType)
                    .MaxBy(target => target.CellPos.Z);
                if (target != null)
                {
                    Room.Push(AddBuffAction, BuffId.DefenceBuff,
                        BuffParamType.Constant, target, this, DefenceBuffParam, 5000, false);
                }
            }
            
            Mp = 0;
        });
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
        }
        else
        {
            State = Mp >= MaxMp && _heal ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
    }
}