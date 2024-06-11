using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Creature : GameObject
{
    protected virtual Skill NewSkill { get; set; }
    protected Skill Skill;
    protected readonly Scheduler Scheduler = new();
    protected readonly List<Skill> SkillList = new();
    protected ProjectileId CurrentProjectile = ProjectileId.BasicProjectile;
    protected bool StateChanged;
    protected bool IsAttacking;
    protected bool AttackEnded = true;
    protected long LastAnimEndTime;
    protected float AttackImpactMoment = 0.5f;
    protected float SkillImpactMoment = 0.5f;
    protected float SkillImpactMoment2 = 0.5f;
    protected const long MpTime = 1000;
    protected const long StdAnimTime = 1000;
    
    public UnitId UnitId { get; set; }

    public override State State
    {
        get => PosInfo.State;
        set
        {
            PosInfo.State = value;
            BroadcastState();
        }
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
            UpdateSkill();
            Mp = 0;
        }
        else
        {
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
                case State.Attack2:
                    UpdateAttack2();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.Skill2:
                    UpdateSkill2();
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
    }
    
    protected virtual void UpdateIdle() { }
    protected virtual void UpdateMoving() { }
    
    protected virtual void UpdateAttack()
    {
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        // 첫 UpdateAttack Cycle시 아래 코드 실행
        if (IsAttacking) return;
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactMoment = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactMoment);
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        long impactMomentCorrection = LastAnimEndTime - timeNow + impactMoment;
        long animPlayTimeCorrection = LastAnimEndTime - timeNow + animPlayTime;
        long impactTime = AttackEnded ? impactMoment : Math.Min(impactMomentCorrection, impactMoment);
        long animEndTime = AttackEnded ? animPlayTime : Math.Min(animPlayTimeCorrection, animPlayTime);
        AttackImpactEvents(impactTime);
        EndEvents(animEndTime); // 공격 Animation이 끝나면 _isAttacking == false로 변경
        AttackEnded = false;
        IsAttacking = true;
    }   
    
    protected virtual void UpdateAttack2() { }
    protected virtual void UpdateSkill() { }
    protected virtual void UpdateSkill2() { }
    protected virtual void UpdateKnockBack() { }
    protected virtual void UpdateRush() { }
    protected virtual void UpdateDie() { }
    public virtual void RunSkill() { }

    protected virtual async void AttackImpactEvents(long impactTime)
    {
        if (Target == null || Room == null || Hp <= 0) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            ApplyAttackEffect(Target);
        });
    }

    protected virtual async void SkillImpactEvents(long impactTime) { }

    protected virtual async void MotionChangeEvents(long time) { }

    protected virtual async void EndEvents(long animEndTime)
    {
        await Scheduler.ScheduleEvent(animEndTime, () =>
        {
            if (Room == null || Hp <= 0) return;
            SetNextState();
            LastAnimEndTime = Room.Stopwatch.ElapsedMilliseconds;
            IsAttacking = false;
        });
    }
    
    public virtual void ApplyAttackEffect(GameObject target)
    {
        if (Room == null || Hp <= 0) return;
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    
    public virtual void ApplyAdditionalAttackEffect(GameObject target) { }
    public virtual void ApplyEffectEffect() { }

    public virtual void ApplyProjectileEffect(GameObject target, ProjectileId pid)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    public virtual void ApplyAdditionalProjectileEffect(GameObject target) { }

    public virtual void SetNextState()
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
            State = State.Attack;
            SetDirection();
        }
    }

    public virtual void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHp();
            // 부활 Effect 추가
        }
    }
    
    protected virtual void SetDirection()
    {
        if (Room == null || Target == null) return;
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastPos();
    }

    public virtual void OnSkillUpgrade(Skill skill)
    {
        var skillName = skill.ToString();
        var name = UnitId.ToString();
        if (skillName.Contains(name) == false) return;
        NewSkill = skill;
        SkillList.Add(NewSkill);
    }

    public virtual void SkillInit()
    {
        var skillUpgradedList = Player.SkillUpgradedList;
        var name = UnitId.ToString();
        if (skillUpgradedList.Count == 0) return;
        
        foreach (var skill in skillUpgradedList)
        {
            var skillName = skill.ToString();
            if (skillName.Contains(name)) SkillList.Add(skill);
        }
        
        if (SkillList.Count == 0) return;
        foreach (var skill in SkillList) NewSkill = skill;
    }
    
    protected virtual State GetRandomState(State state1, State state2)
    {
        return new Random().Next(2) == 0 ? state1 : state2;
    }
    
    protected virtual Vector3 GetRandomDestInFence()
    {
        List<Vector3> sheepBound = GameData.SheepBounds;
        float minX = sheepBound.Select(v => v.X).ToList().Min() + 1.0f;
        float maxX = sheepBound.Select(v => v.X).ToList().Max() - 1.0f;
        float minZ = sheepBound.Select(v => v.Z).ToList().Min() + 1.0f;
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max() - 1.0f;

        do
        {
            Random random = new();
            Map map = Room!.Map;
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));
            bool canGo = map.CanGo(this, map.Vector3To2(dest));
            float dist = Vector3.Distance(CellPos, dest);
            if (canGo && dist > 3f) return dest;
        } while (true);
    }
}