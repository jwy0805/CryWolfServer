using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Horror : Creeper
{
    private bool _poisonImmunity = false;
    private bool _rollPoison = false;
    private bool _poisonSmog = false;
    private bool _division = false;
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
        Player.SkillSubject.SkillUpgraded(Skill.HorrorRollPoison);
        Player.SkillSubject.SkillUpgraded(Skill.HorrorDegeneration);
        Player.SkillSubject.SkillUpgraded(Skill.HorrorDivision);
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
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.BigPoison, this, 5f);
        });
    }

    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0) return;
        var targetPos = target.CellPos;
        BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0.05f, 5000, true);
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        
        if (_poisonSmog == false || Mp < MaxMp) return;
        Mp = 0;
        _poisonSmogPos = new PositionInfo { PosX = targetPos.X, PosY = targetPos.Y + 0.5f, PosZ = targetPos.Z };
        Room.SpawnEffect(EffectId.PoisonSmog, this, _poisonSmogPos, false, 4000);
    }
    
    public override void ApplyEffectEffect()
    {
        if (Room == null) return;
        var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var effectCellPos = new Vector3(_poisonSmogPos.PosX, _poisonSmogPos.PosY, _poisonSmogPos.PosZ);
        var targets = Room.FindTargets(effectCellPos, types, _poisonSmogRange);
        
        foreach (var target in targets)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, target, this, 0.05f, 5000, true);
        }
    }

    protected override void ApplyRollEffect(GameObject? target)
    {
        if (target == null || Room == null) return;
        
        target.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        if (_rollPoison == false) return;
        Room.SpawnEffect(EffectId.HorrorRoll, this, PosInfo);
        var types = new[] { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var targets = Room.FindTargetsInAngleRange(this, types, 5, 60);
        foreach (var gameObject in targets)
        {
            BuffManager.Instance.AddBuff(BuffId.Addicted, gameObject, this, 0.05f, 5000, true);
            gameObject.OnDamaged(this, TotalSkillDamage / 2, Damage.Normal);
        }
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (damageType is Damage.Poison && _poisonImmunity) return;
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

    protected override void OnDead(GameObject? attacker)
    {
        Player.SkillSubject.RemoveObserver(this);
        if (Room == null) return;
        
        Targetable = false;
        if (attacker != null)
        {
            attacker.KillLog = Id;
            if (attacker.Target != null)
            {
                if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
                {
                    if (attacker.Parent != null) attacker.Parent.Target = null;
                }
                attacker.Target = null;
            }
        }
        
        if (AlreadyRevived == false && WillRevive)
        {
            if (IsAttacking) IsAttacking = false;
            if (AttackEnded == false) AttackEnded = true;  
            Room.Broadcast(new S_Die { ObjectId = Id, Revive = true});
            return;
        }

        if (Degeneration)
        {
            Room.Map.ApplyLeave(this);
            
            var creeperPos = new PositionInfo
            {
                PosX = PosInfo.PosX, PosY = PosInfo.PosY + BounceParam, PosZ = PosInfo.PosZ,
                State = State.Divide,
                Dir = Dir
            };

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