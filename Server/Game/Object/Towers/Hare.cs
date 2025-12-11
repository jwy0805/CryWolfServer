using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Hare : Rabbit
{
    private bool _punch = false;
    private bool _defenceDown = false;
    
    private readonly int _defenceDecreaseParam = (int)DataManager.SkillDict[(int)Skill.HarePunchDefenceDown].Value;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HarePunch:
                    _punch = true;
                    break;
                case Skill.HarePunchDefenceDown:
                    _defenceDown = true;
                    break;
                case Skill.HareEvasion:
                    Evasion += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && Target != null)
            {
                if (Vector3.Distance(Target.CellPos with { Y = 0 }, CellPos with { Y = 0 }) > TotalSkillRange)
                {
                    State = State.Idle;
                    return;
                }
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
        if (Room == null) return;
        // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        Vector3 flatTargetPos = Room.Map.GetClosestPoint(this, Target) with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);

        if (distance > TotalAttackRange) return;
        State = State.Attack;
        SyncPosAndDir();
    }
    
    protected override void OnSkill()
    {
        if (Target == null || Target.Targetable == false) return;
        base.OnSkill();
        AttackEnded = false;
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            Room.SpawnProjectile(ProjectileId.RabbitAggro, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;

            if (_punch)
            {
                var targets  = Room.FindTargets(
                        this, new [] { GameObjectType.Monster }, TotalSkillRange)
                    .Where(target => target.Targetable)
                    .OrderBy(_ => Guid.NewGuid()).Take(1).ToList();

                if (targets.Any())
                {
                    var target = targets.First();
                    Vector3 dir = target.CellPos - CellPos;
                    dir = Vector3.Normalize(dir);
                    Vector3 spawnPos = CellPos + dir * TotalAttackRange;
                    var posInfo = new PositionInfo
                    {
                        PosX = spawnPos.X, PosY = spawnPos.Y, PosZ = spawnPos.Z, State = State.Skill2, Dir = Dir
                    };
                    var vector = Room.Map.Vector3To2(new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ));

                    if (Room.Map.CanGo(this, vector) == false)
                    {
                        var newVector2 = Room.Map.FindNearestEmptySpace(vector, this);
                        var newVector3 = Room.Map.Vector2To3(newVector2);
                        posInfo.PosX = newVector3.X;
                        posInfo.PosZ = newVector3.Z;
                    }
                
                    SpawnClone(posInfo, Player);
                    Room.SpawnEffect(EffectId.HareEffect, this, this, PosInfo, false, (int)(StdAnimTime / TotalAttackSpeed));
                }
            }
            else
            {
                Room.SpawnProjectile(ProjectileId.HarePunch, this, 5f);
            }
            
            Mp = 0;
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0 || AddBuffAction == null) return;
        if (pid == ProjectileId.HarePunch)
        {
            if (target is not Creature creature) return;
            Room.Push(AddBuffAction, BuffId.Aggro, BuffParamType.None, creature, this, 0, 2000, false);
            if (_defenceDown)
            {
                creature.DefenceParam -= _defenceDecreaseParam;
            }
        }
        else
        {
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        }
    }

    private void SpawnClone(PositionInfo posInfo, Player player)
    {
        var tower = ObjectManager.Instance.Add<HareClone>();
        var clonePos = new PositionInfo
        {
            PosX = posInfo.PosX, PosY = posInfo.PosY, PosZ = posInfo.PosZ, State = State.Skill2, Dir = Dir
        };

        tower.PosInfo = clonePos;
        tower.Info.PosInfo = tower.PosInfo;
        tower.Info.Name = UnitId.Hare.ToString();
        tower.Player = player;
        tower.UnitId = UnitId.Hare;
        tower.Room = Room;
        tower.Parent = this;
        tower.Init();
        Room?.Push(Room.EnterGame, tower);
        Room?.SpawnEffect(EffectId.HareCloneEffect, this, this, clonePos);
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
    
        State = _punch && Mp >= MaxMp ? State.Skill : State.Attack;
        SyncPosAndDir();
    }
}

public class HareClone : Rabbit
{
    public override void Init()
    {
        if (Room == null || Parent == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds;
        Targetable = false;
        MaxHp = Parent.MaxHp;
        Hp = Parent.Hp;
        AttackSpeed = Parent.TotalAttackSpeed;
        Accuracy = Parent.TotalAccuracy;
        SkillDamage = Parent.TotalSkillDamage;
        UnitRole = Role.Warrior;
        
        Target = Parent.Target;
        WillRevive = false;

        SkillImpactMoment = 0.35f;
        
        Room.Broadcast(new S_SetAnimSpeed { ObjectId = Id, SpeedParam = TotalAttackSpeed });
        SyncPosAndDir();

        Room.PushAfter(CallCycle, OnSkill);
    }

    public override void Update()
    {
        if (Room == null || Parent == null) return;
        if (Parent.Hp <= 0)
        {
            OnDead(null);
            return;
        }
        Job = Room.PushAfter(CallCycle, Update);
        
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
            case State.Skill2:
                UpdateSkill2();
                break;
        }
    }

    protected override void UpdateSkill2()
    {
        UpdateSkill();
    }
    
    protected override async void SkillImpactEvents(long impactTime)
    {
        try
        {
            await Scheduler.ScheduleEvent(impactTime, () =>
            {
                if (Target == null || Target.Targetable == false || Room == null || Parent is not { Hp: > 0 })
                {
                    Room?.PushAfter(800, Room.LeaveGame, Id);
                    return;
                }

                Room.Push(Target.OnDamaged, Parent, TotalSkillDamage, Damage.Normal, false);
                Action<BuffId, BuffParamType, GameObject, Creature, float, long, bool> addBuffAction = Room.AddBuff;
                Room.Push(addBuffAction, BuffId.Aggro,
                    BuffParamType.None, Target, (Creature)Parent, 0, 2000, false);
                Room.PushAfter(800, Room.LeaveGame, Id);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}