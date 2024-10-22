using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Horror : Creeper
{
    private bool _poisonImmunity;
    private bool _rollPoison;
    private bool _poisonSmog;
    private bool _division;
    private readonly int _divisionNum = 2;
    private readonly float _poisonSmogRange = 3;
    private PositionInfo _poisonSmogPos = new();
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HorrorPoisonSmog:
                    _poisonSmog = true;
                    break;
                case Skill.HorrorPoisonImmunity:
                    _poisonImmunity = true;
                    PoisonResistParam += 100;
                    break;
                case Skill.HorrorRollPoison:
                    _rollPoison = true;
                    break;
                case Skill.HorrorDegeneration:
                    Degeneration = true;
                    break;
                case Skill.HorrorDivision:
                    _division = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Ranger;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
        }

        switch (State)
        {
            case State.Die:
                UpdateDie();
                break;
            case State.Moving:
                UpdateMoving();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Rush:
                UpdateRush();
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
        if (Room == null) return;
        
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        if (Rushed == false)
        {
            MoveSpeed += RushSpeed;
            State = State.Rush; 
        }
        else
        {
            State = State.Moving;
        }
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.BigPoison, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        var targetPos = target.CellPos;

        Room.Push(AddBuffAction, BuffId.Addicted,
            BuffParamType.Percentage, target, this, 0.05f, 5000, true);
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        
        if (_poisonSmog == false || Mp < MaxMp) return;
        Mp = 0;
        _poisonSmogPos = new PositionInfo { PosX = targetPos.X, PosY = targetPos.Y + 0.5f, PosZ = targetPos.Z };
        Room.SpawnEffect(EffectId.PoisonSmog, this, this, _poisonSmogPos, false, 4000);
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null || AddBuffAction == null) return;
        var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var effectCellPos = new Vector3(_poisonSmogPos.PosX, _poisonSmogPos.PosY, _poisonSmogPos.PosZ);
        var targets = Room.FindTargets(effectCellPos, types, _poisonSmogRange);
        
        foreach (var target in targets)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, 0.05f, 5000, true);
        }
    }

    protected override void ApplyRollEffect(GameObject? target)
    {
        if (target == null || Room == null || AddBuffAction == null) return;
        
        Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Normal, false);
        if (_rollPoison == false) return;
        
        Room.SpawnEffect(EffectId.HorrorRoll, this, this, PosInfo);
        var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var targets = Room.FindTargetsInAngleRange(this, SavedDir, types, 5, 60);
        
        foreach (var gameObject in targets)
        {
            Room.Push(AddBuffAction, BuffId.Addicted,
                BuffParamType.Percentage, target, this, 0.05f, 5000, true);
            Room.Push(gameObject.OnDamaged, this, TotalSkillDamage / 2, Damage.Normal, false);
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (damageType is Damage.Poison && _poisonImmunity) return;
        
        var random = new Random();
        if (random.Next(100) > attacker.TotalAccuracy - TotalEvasion && damageType is Damage.Normal)
        {   // Evasion
            // TODO: Evasion Effect
            return;
        }
        
        // 일반적으로 Normal Damage 만 Critical 가능, Magical이나 True Damage Critical 구현 시 데미지를 넣는 Unit으로부터 자체적으로 계산
        var totalDamage = random.Next(100) < attacker.CriticalChance && damageType is Damage.Normal
            ? (int)(damage * attacker.CriticalMultiplier) : damage;
        
        if (ShieldRemain > 0)
        {   
            // Shield
            ShieldRemain -= totalDamage;
            if (ShieldRemain < 0)
            {
                totalDamage = Math.Abs(ShieldRemain);
                ShieldRemain = 0;
            }
        }

        totalDamage = GameManager.Instance.CalcDamage(this, damageType, totalDamage);
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        
        if (Hp <= 0)
        {   // Dead
            OnDead(attacker);
            return;
        }
        
        if (damageType is Damage.Normal && Reflection && reflected == false && attacker.Targetable)
        {   // Reflection
            var reflectionDamage = (int)(totalDamage * ReflectionRate / 100);
            Room.Push(attacker.OnDamaged, this, reflectionDamage, damageType, true);
        }
    }

    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        Scheduler.CancelEvent(AttackTaskId);
        Scheduler.CancelEvent(EndTaskId);
        if (Room == null) return;
        
        Targetable = false;
        State = State.Die;
        Room.RemoveAllBuffs(this);
        
        if (attacker != null)
        {
            attacker.KillLog = Id;
            attacker.Target = null;
            
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile && attacker.Parent != null)
            {
                attacker.Parent.Target = null;
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (AttackEnded == false) AttackEnded = true;  
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true});
            return;
        }

        // Degeneration to Creeper
        if (Degeneration)
        {
            Room.Map.ApplyLeave(this);
            
            var creeperPos = new PositionInfo
            {
                PosX = PosInfo.PosX, PosY = PosInfo.PosY + BounceParam, PosZ = PosInfo.PosZ,
                State = State.Divide,
                Dir = Dir
            };
            
            // Division to two Creepers
            if (_division)
            {
                for (int i = 0; i < _divisionNum; i++)
                {
                    var creeper = (Creeper)Room.SpawnMonster(UnitId.Creeper, creeperPos, Player);
                    creeper.Degeneration = true;
                    creeper.OnDivide();
                }
            }
            else
            {
                var monster = (Creeper)Room.SpawnMonster(UnitId.Creeper, creeperPos, Player);
                monster.Degeneration = true;
                Room.LeaveGame(Id);
                return;
            }
        }
        
        Room.Broadcast(new S_Die { ObjectId = Id });
        Room.DieAndLeave(Id);
    }
}