using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SoulMage : Haunt
{
    private bool _dragonPunch = false;
    private bool _shareDamage = false;
    private bool _magicPortal = false;
    private bool _debuffResist = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SoulMageDragonPunch:
                    _dragonPunch = true;
                    break;
                case Skill.SoulMageShareDamage:
                    _shareDamage = true;
                    break;
                case Skill.SoulMageMagicPortal:
                    _magicPortal = true;
                    break;
                case Skill.SoulMageDebuffResist:
                    _debuffResist = true;
                    break;
                case Skill.SoulMageCritical:
                    CriticalChance += 25;
                    CriticalMultiplier = 1.5f;
                    break;
                
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
        
        Player.SkillSubject.SkillUpgraded(Skill.SoulMageDragonPunch);
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
            case State.Rush:
                UpdateRush();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }
    }
    
    // public override void Update()
    // {
    //     if (Room == null) return;
    //     Job = Room.PushAfter(CallCycle, Update);
    //     
    //     if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime * 5 && _natureAttack)
    //     {
    //         Time = Room!.Stopwatch.ElapsedMilliseconds;
    //         List<GameObjectType> typeList = new() { GameObjectType.Monster };
    //         List<Creature> monsters = Room.FindTargets(this, typeList, AttackRange, 2).Cast<Creature>().ToList();
    //         if (monsters.Any())
    //         {
    //             Creature monster = monsters.OrderBy(_ => Guid.NewGuid()).ToList().First();
    //             Effect greenGate = ObjectManager.Instance.CreateEffect(EffectId.GreenGate);
    //             greenGate.Room = Room;
    //             greenGate.Parent = this;
    //             greenGate.Target = monster;
    //             greenGate.PosInfo = monster.PosInfo;
    //             greenGate.Info.PosInfo = monster.Info.PosInfo;
    //             greenGate.Info.Name = nameof(EffectId.GreenGate);
    //             greenGate.Init();
    //             Room.EnterGameTarget(greenGate, greenGate.Parent, monster);
    //         }
    //     }
    //     
    //     switch (State)
    //     {
    //         case State.Die:
    //             UpdateDie();
    //             break;
    //         case State.Moving:
    //             UpdateMoving();
    //             break;
    //         case State.Idle:
    //             UpdateIdle();
    //             break;
    //         case State.Rush:
    //             UpdateRush();
    //             break;
    //         case State.Attack:
    //             UpdateAttack();
    //             break;
    //         case State.Skill:
    //             UpdateSkill();
    //             break;
    //         case State.Skill2:
    //             UpdateSkill2();
    //             break;
    //         case State.KnockBack:
    //             UpdateKnockBack();
    //             break;
    //         case State.Faint:
    //             break;
    //         case State.Standby:
    //             break;
    //     }   
    // }
    
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
        
        if (_magicPortal && Mp >= MaxMp)
        {
            State = State.Skill;
            return;
        }

        if (distance > TotalAttackRange) return;
        State = State.Attack;
        SyncPosAndDir();
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {   
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            
            if (_dragonPunch)
            {
                var effectPos = new PositionInfo
                { 
                    PosX = CellPos.X, PosY = CellPos.Y, PosZ = CellPos.Z, Dir = Dir
                };
                
                Room.SpawnEffect(EffectId.SoulMagePunch, this, effectPos);
            }
            else
            {
                Room.SpawnProjectile(ProjectileId.SoulMageProjectile, this, 5f);
            }
        });
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null) return;
        var types = new[] { GameObjectType.Monster, GameObjectType.MonsterStatue };
        var targets = Room.FindTargetsInRectangle(this,
            types, 2, 6, Dir, 2);
        foreach (var target in targets)
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 0, 5000);
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        
        int totalDamage = attacker.CriticalChance > 0 
            ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
            : Math.Max(damage - TotalDefence, 0);
        
        if (_shareDamage)
        {
            // List<UnitId> towerIds = new() 
            //     { ((Tower)this).UnitId.PracticeDummy, ((Tower)this).UnitId.TargetDummy, ((Tower)this).UnitId.TrainingDummy }; 
            // GameObject? nearestDummy = Room.FindNearestTower(towerIds);
            // if (nearestDummy == null)
            // {
            //     Hp = Math.Max(Stat.Hp - damage, 0);
            // }
            // else
            // {
            //     damage = (int)(damage * 0.5f);
            //     nearestDummy.OnDamaged(attacker, damage);
            //     Hp = Math.Max(Stat.Hp - damage, 0);
            // }
        }
        else
        {
            Hp = Math.Max(Stat.Hp - totalDamage, 0);
        }
        
        if (Reflection && reflected == false)
        {
            int refParam = (int)(damage * ReflectionRate);
            attacker.OnDamaged(this, refParam, damageType, true);
        }
        
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp <= 0) OnDead(attacker);
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

        State = _magicPortal && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}