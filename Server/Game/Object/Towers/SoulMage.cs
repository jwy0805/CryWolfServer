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
    private GameObject? _effectTarget;
    
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
        Player.SkillSubject.SkillUpgraded(Skill.SoulMageShareDamage);
        Player.SkillSubject.SkillUpgraded(Skill.SoulMageMagicPortal);
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            
            if (Mp >= MaxMp && _magicPortal)
            {
                var effectPos = new PositionInfo
                { 
                    PosX = CellPos.X, PosY = CellPos.Y + 4, PosZ = CellPos.Z, Dir = Dir
                };
                Room.SpawnEffect(EffectId.GreenGate, this, effectPos, false, 3500);
                Mp = 0;
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

        if (distance > TotalAttackRange) return;
        State = _dragonPunch ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {   
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SoulMageProjectile, this, 5f);
        });
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            var effectPos = new PositionInfo
            { 
                PosX = CellPos.X, PosY = CellPos.Y, PosZ = CellPos.Z, Dir = Dir
            };
                
            Room?.SpawnEffect(EffectId.SoulMagePunch, this, effectPos);
        });
    }

    private async void NaturalTornadoEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (_effectTarget == null) return;
            BuffManager.Instance.AddBuff(BuffId.Fainted, _effectTarget, this, 0, 1300);
            _effectTarget.OnDamaged(this, (int)(TotalSkillDamage * 0.4), Damage.True);
        });
    }
    
    private async void StarFallEvents(long impactTime, PositionInfo effectPos)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Room == null) return;
            
            var types = new[] { GameObjectType.Monster, GameObjectType.MonsterStatue };
            var effectCellPos = new Vector3(effectPos.PosX, effectPos.PosY, effectPos.PosZ);
            var targets = Room.FindTargets(effectCellPos, types, 3.5f, 2);
            if (targets.Any() == false) return;
            
            foreach (var target in targets)
            {
                target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            }
        });
    }
    
    private async void PurpleBeamEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            _effectTarget?.OnDamaged(this, (int)(TotalSkillDamage * 0.7), Damage.Magical);
        });
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null) return;

        float dir;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            dir = Dir;
        }
        else
        {
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        }
        
        var types = new[] { GameObjectType.Monster, GameObjectType.MonsterStatue };
        var targets = Room.FindTargetsInRectangle(this,
            types, 2, 7, dir, 2);
        foreach (var target in targets)
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 0, 5000);
        }
    }

    public override void ApplyEffectEffect(EffectId eid)
    {
        if (Room == null) return;
        if (eid != EffectId.GreenGate) return;
        
        var random = new Random().Next(3);
        var types = new List<GameObjectType> { GameObjectType.Monster };
        _effectTarget = Room.FindRandomTarget(this, types, TotalSkillRange, 2);
        if (_effectTarget == null)
        {   // 몬스터가 우선순위 1이므로 먼저 찾고 그 다음 석상 탐색
            types = new List<GameObjectType> { GameObjectType.MonsterStatue };
            _effectTarget = Room.FindRandomTarget(this, types, TotalSkillRange, 2);
            if (_effectTarget == null) return;
        }
                
        var effectPos = new PositionInfo
        { 
            PosX = _effectTarget.CellPos.X, PosY = _effectTarget.CellPos.Y, PosZ = _effectTarget.CellPos.Z, Dir = Dir
        };
                
        switch (random)
        {
            case 0:
                Room.SpawnEffect(EffectId.NaturalTornado, _effectTarget, effectPos, true, 3000);
                NaturalTornadoEvents(100);
                NaturalTornadoEvents(800);
                NaturalTornadoEvents(1500);
                NaturalTornadoEvents(2200);
                break;
            case 1:
                Room.SpawnEffect(EffectId.StarFall, this, effectPos, false, 3000);
                StarFallEvents(500, effectPos);
                StarFallEvents(1000, effectPos);
                StarFallEvents(1500, effectPos);
                break;
            default:
                Room.SpawnEffect(EffectId.PurpleBeam, _effectTarget, effectPos, true, 4000);
                PurpleBeamEvents(900);
                PurpleBeamEvents(1150);
                PurpleBeamEvents(1400);
                PurpleBeamEvents(1650);
                PurpleBeamEvents(1900);
                PurpleBeamEvents(2150);
                PurpleBeamEvents(2400);
                PurpleBeamEvents(2650);
                PurpleBeamEvents(2900);
                break;
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible || Targetable == false || Hp <= 0) return;
        var random = new Random();
        var totalDamage = damageType is Damage.Normal or Damage.Magical 
            ? Math.Max(damage - TotalDefence, 0) : damage;
        
        if (random.Next(100) < attacker.CriticalChance)
        {
            totalDamage = (int)(totalDamage * attacker.CriticalMultiplier);
        }
        
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {
            // TODO: Evasion Effect
            return;
        }

        if (_shareDamage)
        {
            var type = new[] { GameObjectType.Tower };
            var tanker = Room.FindTargets(this, type, 100, 2)
                .Where(gameObject => gameObject is Creature { UnitRole: Role.Tanker, Hp: > 0, Targetable: true })
                .MinBy(tanker => Vector3.Distance(tanker.CellPos, CellPos));
            
            if (tanker != null)
            {
                var halfDamage = (int)(totalDamage * 0.5f);
                tanker.OnDamaged(attacker, halfDamage, damageType);
                totalDamage = halfDamage;
            }
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            attacker.OnDamaged(this, reflectionDamage, damageType, true);
        }
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

        State = _dragonPunch ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}