using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SnakeNaga : Snake
{
    private bool _bigFire;
    private bool _drain;
    private bool _meteor;
    private readonly float _meteorRange = 3f;
    private readonly float _drainParam = 0.2f;
    private PositionInfo _meteorPos = new();
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeNagaBigFire:
                    Attack += 20;
                    _bigFire = true;
                    break;
                case Skill.SnakeNagaDrain:
                    _drain = true;
                    break;
                case Skill.SnakeNagaCritical:
                    CriticalChance += 30;
                    break;
                case Skill.SnakeNagaSuperAccuracy:
                    Accuracy += 70;
                    break;
                case Skill.SnakeNagaMeteor:
                    _meteor = true;
                    break;
            }
        }
    }
    
    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
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
    
    protected override void UpdateMoving()
    {
        if (Room == null) return;
        
        // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   
            // Target이 없거나 타겟팅이 불가능한 경우
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
            State = Mp >= MaxMp && _meteor && Target != null ? State.Skill : State.Attack;
            SyncPosAndDir();
            return;
        }
        
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        if (Path.Count == 0)
        {
            State = State.Idle;
            BroadcastPos();
            UnreachableIds.Add(Target.Id);
            return;
        }
        BroadcastPath();
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(_bigFire ? ProjectileId.SnakeNagaBigFire : ProjectileId.SnakeNagaFire,
                this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            AttackEnded = true;
            var searchType = new List<GameObjectType>
            {
                GameObjectType.Sheep, GameObjectType.Tower
            };
            var targetType = new List<GameObjectType>
            {
                GameObjectType.Sheep, GameObjectType.Tower, GameObjectType.Fence
            };
            var meteorTarget = Room.FindDensityTargets(searchType, targetType, 
                this, TotalSkillRange + _meteorRange, _meteorRange);
            if (meteorTarget == null)
            {
                _meteorPos = new PositionInfo
                {
                    PosX = Target.PosInfo.PosX, PosY = Target.PosInfo.PosY, PosZ = Target.PosInfo.PosZ
                };
            }
            else
            {
                _meteorPos = new PositionInfo
                {
                    PosX = meteorTarget.PosInfo.PosX, PosY = meteorTarget.PosInfo.PosY, PosZ = meteorTarget.PosInfo.PosZ
                };
            }
           
            Room.SpawnEffect(EffectId.Meteor, this, this, _meteorPos, false, 5000);
            Mp = 0;
        });
    }
    
    public override void ApplyProjectileEffect(GameObject target, ProjectileId _)
    {
        if (Room == null || AddBuffAction == null) return;
        
        if (_drain)
        {
            var damage = Math.Max(TotalAttack - target.TotalDefence, 0);
            Hp += (int)(damage * _drainParam);
            BroadcastHp();
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
    }

    public override void ApplyEffectEffect()
    {
        if (Room == null || AddBuffAction == null) return;
        var meteorPos = new Vector3(_meteorPos.PosX, _meteorPos.PosY, _meteorPos.PosZ);
        var types = new HashSet<GameObjectType> { GameObjectType.Sheep, GameObjectType.Fence, GameObjectType.Tower };
        var targets = Room.FindTargets(meteorPos, types, _meteorRange, 2);
        foreach (var target in targets)
        {
            Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            Room.Push(AddBuffAction, BuffId.Burn, BuffParamType.None, target, this, 0, 5000, false);
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
            return;
        }
        
        State = _meteor && Mp >= MaxMp && Target != null ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}