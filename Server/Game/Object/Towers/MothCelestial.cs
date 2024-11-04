using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class MothCelestial : MothMoon
{
    private bool _poison;
    private bool _breedSheep;
    private bool _debuffRemove;
    private readonly int _breedProb = 10;

    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothCelestialSheepHealParamUp:
                    HealParam = 300;
                    break;
                case Skill.MothCelestialPoison:
                    _poison = true;
                    break;
                case Skill.MothCelestialAccuracy:
                    Accuracy += 30;
                    break;
                case Skill.MothCelestialBreed:
                    _breedSheep = true;
                    break;
                case Skill.MothCelestialSheepDebuffRemove:
                    _debuffRemove = true;
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
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime  && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp)
            {
                State = State.Skill;
                Mp = 0;
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
        State = Mp >= MaxMp ? State.Skill : State.Attack;
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
            
            Room.SpawnProjectile(_poison ? ProjectileId.MothCelestialPoison : ProjectileId.MothMoonProjectile,
                this, 5f);
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
                // Heal Sheep
                var sheepHeal = sheeps.MinBy(sheep => sheep.Hp);
                if (sheepHeal != null) sheepHeal.Hp += HealParam;
            
                // Shield Sheep
                var sheepShield = sheeps.MinBy(_ => Guid.NewGuid());
                if (sheepShield != null) sheepShield.ShieldAdd += ShieldParam;

                // Debuff Remove
                if (_debuffRemove)
                {
                    var sheepDebuff = Room.Buffs.Where(buff => buff.Master is Sheep)
                        .Select(buff => buff.Master as Sheep)
                        .Distinct()
                        .Where(s => s != null && Vector3.Distance(
                            s.CellPos with { Y = 0 }, CellPos with { Y = 0 }) <= TotalSkillRange)
                        .ToList();
                    
                    foreach (var sheep in sheepDebuff)
                    {
                        if (sheep is { Room: not null }) Room.Push(Room.RemoveAllDebuffs, sheep);
                    }
                }
                else
                {
                    var sheep = Room.Buffs.Where(buff => buff.Master is Sheep)
                        .Select(buff => buff.Master as Sheep)
                        .Distinct()
                        .Where(s => s != null && Vector3.Distance(
                            s.CellPos with { Y = 0 }, CellPos with { Y = 0 }) <= TotalSkillRange)
                        .MinBy(_ => Guid.NewGuid());
                
                    if (sheep is { Room: not null }) Room.Push(Room.RemoveAllDebuffs, sheep);
                }
            
                // Breed Sheep
                if (_breedSheep && new Random().Next(99) < _breedProb)
                {
                    Room.SpawnSheep(Player);
                }
            }
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        if (Room == null || AddBuffAction == null) return;
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        
        if (_poison)
        {
            Room.Push(AddBuffAction, BuffId.Addicted, 
                BuffParamType.Percentage, target, this, 0.05f, 5000, false);
        }
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
}