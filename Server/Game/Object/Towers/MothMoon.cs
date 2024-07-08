using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothMoon : MothLuna
{
    private bool _sheepDebuffRemove;
    private bool _sheepHeal;
    private bool _sheepshield;
    
    protected readonly int HealParam = 80;
    protected readonly int ShieldParam = 100;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothMoonSheepHeal:
                    _sheepHeal = true;
                    break;
                case Skill.MothMoonSheepShield:
                    _sheepshield = true;
                    break;
                case Skill.MothMoonSheepDebuffRemove:
                    _sheepDebuffRemove = true;
                    break;
                case Skill.MothMoonAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.2f;
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
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.MothMoonProjectile, this, 5f);
        });
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
        State = (_sheepshield || _sheepHeal) && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            
            var types = new[] { GameObjectType.Sheep };
            var sheeps = Room.FindTargets(this, types, AttackRange);

            if (sheeps.Any() == false) return;
            if (_sheepHeal)
            {
                var sheep = sheeps.MinBy(sheep => sheep.Hp);
                if (sheep != null) sheep.Hp += HealParam;
            }
            
            if (_sheepshield)
            {
                var sheep = sheeps.MinBy(_ => Guid.NewGuid());
                if (sheep != null) sheep.ShieldAdd += ShieldParam;
            }

            if (_sheepDebuffRemove)
            {
                var sheep = BuffManager.Instance.Buffs.Where(buff => buff.Master is Sheep)
                    .MinBy(_ => Guid.NewGuid())?.Master;
                if (sheep is Creature creature && sheep.Room != null) BuffManager.Instance.RemoveAllDebuff(creature);
            }
        });
    }
    
    protected override void SetNextState()
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
            State = Mp >= MaxMp && (_sheepshield || _sheepHeal) ? State.Skill : State.Attack;
            SyncPosAndDir();
        }
    }
}