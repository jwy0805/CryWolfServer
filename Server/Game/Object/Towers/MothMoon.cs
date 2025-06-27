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
    private bool _sheepShield;
    
    protected int HealParam = 80;
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
                    _sheepShield = true;
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
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && (_sheepShield || _sheepHeal))
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
        State = (_sheepShield || _sheepHeal) && Mp >= MaxMp ? State.Skill : State.Attack;
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

            Room.SpawnProjectile(ProjectileId.MothMoonProjectile, this, 5f);
        });
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            
            var types = new[] { GameObjectType.Sheep };
            var sheeps = Room.FindTargets(this, types, TotalSkillRange);

            if (sheeps.Any())
            {
                if (_sheepHeal)
                {
                    var sheep = sheeps.MinBy(sheep => sheep.Hp);
                    if (sheep != null) sheep.Hp += HealParam;
                }
            
                if (_sheepShield)
                {
                    var sheep = sheeps.MinBy(_ => Guid.NewGuid());
                    if (sheep != null)
                    {
                        sheep.ShieldAdd += sheep.ShieldRemain > sheep.MaxHp ? 0 : ShieldParam;
                    }
                }

                if (_sheepDebuffRemove)
                {
                    var sheep = Room.Buffs.Where(buff => buff.Master is Sheep)
                        .Select(buff => buff.Master as Sheep)
                        .Distinct()
                        .Where(s => s != null && Vector3.Distance(
                            s.CellPos with { Y = 0 }, CellPos with { Y = 0 }) <= TotalSkillRange)
                        .MinBy(_ => Guid.NewGuid());
                
                    if (sheep is { Room: not null }) Room.Push(Room.RemoveAllDebuffs, sheep);
                }
            }
            
            Mp = 0;
        });
    }
}