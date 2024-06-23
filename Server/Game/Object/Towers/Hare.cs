using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Hare : Rabbit
{
    private bool _punch = false;
    private bool _defenceDown = false;
    
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
                    Evasion += 12;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        Player.SkillSubject.SkillUpgraded(Skill.HarePunch);
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
        
        if (_punch && distance < TotalSkillRange && Mp >= MaxMp)
        {
            State = State.Skill;
            SyncPosAndDir();
            return;
        }
        
        if (distance < TotalAttackRange)
        {
            State = State.Attack;
        }
        
        SyncPosAndDir();
    }
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(Mp >= MaxMp ?
                ProjectileId.HarePunch : ProjectileId.RabbitAggro, this, 5f);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;

            if (_punch)
            {
                Vector3 dir = Target.CellPos - CellPos;
                dir = Vector3.Normalize(dir);
                Vector3 spawnPos = CellPos + dir * TotalAttackRange;
                var posInfo = new PositionInfo
                {
                    PosX = spawnPos.X, PosY = spawnPos.Y, PosZ = spawnPos.Z, State = State.Skill2, Dir = Dir
                };
                SpawnClone(posInfo, Player);
                Mp = 0;
            }
            else
            {
                Room.SpawnProjectile(ProjectileId.HarePunch, this, 5f);
            }
        });
    }
    
    public override void ApplyProjectileEffect(GameObject? target, ProjectileId pid)
    {
        if (Room == null || target == null || Hp <= 0) return;
        if (pid == ProjectileId.HarePunch)
        {
            if (target is not Creature creature) return;
            BuffManager.Instance.AddBuff(BuffId.Aggro, creature, this, 0, 2000);
            Mp = 0;
        }
        else
        {
            target.OnDamaged(this, TotalAttack, Damage.Normal);
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
    }
}

public class HareClone : Rabbit
{
    public override void Init()
    {
        if (Room == null || Parent == null) return;
        Time = Room.Stopwatch.ElapsedMilliseconds;
        MaxHp = Parent.MaxHp;
        Hp = Parent.Hp;
        AttackSpeed = Parent.AttackSpeed;
        AttackSpeedParam = Parent.AttackSpeedParam;
        Accuracy = Parent.Accuracy;
        
        Target = Parent.Target;
        WillRevive = false;
        
        Room.Broadcast(new S_SetAnimSpeed { ObjectId = Id, SpeedParam = TotalAttackSpeed });
        
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * SkillImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        SkillImpactEvents(impactMoment);
        EndEvents(animPlayTime); // 공격 Animation이 끝나면 사라짐
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
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Parent is not { Hp: > 0 }) return;
            Target.OnDamaged(Parent, TotalSkillDamage, Damage.Normal);
            BuffManager.Instance.AddBuff(BuffId.Aggro, Target, (Creature)Parent, 0, 2000);
        });
    }
    
    protected override void EndEvents(long animPlayTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(animPlayTime, () =>
        {
            if (Room == null) return;
            Room.LeaveGame(Id);
        });
    }
}