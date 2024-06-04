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
    protected long LastAttackTime;
    protected float AttackImpactTime = 0.5f;
    protected float SkillImpactTime = 0.5f;
    protected float SkillImpactTime2 = 0.5f;
    protected const long MpTime = 1000;
    protected const long StdAnimTime = 1000;

    public override State State
    {
        get => PosInfo.State;
        set
        {
            var preState = PosInfo.State;
            PosInfo.State = value;
            var attackStates = new List<State> { State.Attack, State.Skill, State.Skill2 };
            BroadcastState();
            StateChanged = !attackStates.Contains(preState);
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
    protected virtual void UpdateAttack() { }
    protected virtual void UpdateAttack2() { }
    protected virtual void UpdateSkill() { }
    protected virtual void UpdateSkill2() { }
    protected virtual void UpdateKnockBack() { }
    protected virtual void UpdateRush() { }
    protected virtual void UpdateDie() { }
    public virtual void SkillInit() { }
    public virtual void RunSkill() { }

    protected virtual async void AttackImpactEvents(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () => ApplyNormalAttackEffect(Target));
    }

    protected virtual async void SkillImpactEvents(long impactTime) { }

    protected virtual async void MotionChangeEvents(long time) { }

    protected virtual async void EndEvents(long animEndTime)
    {
        await Scheduler.ScheduleEvent(animEndTime, () =>
        {
            IsAttacking = false;
            LastAttackTime = Room.Stopwatch.ElapsedMilliseconds;
            SetNextState();
        });
    }
    
    public virtual void ApplyNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    
    public virtual void ApplyAdditionalAttackEffect(GameObject target) { }
    public virtual void ApplyEffectEffect() { }

    public virtual void ApplyProjectileEffect(GameObject? target)
    {
        target?.OnDamaged(this, TotalAttack, Damage.Normal);
    }
    public virtual void ApplyAdditionalProjectileEffect(GameObject target) { }

    public virtual void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = Vector3.Distance(targetPos, CellPos);

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
        }
        else
        {
            State = State.Attack;
            IsAttacking = false;
        }
    }

    public virtual void SetNextState(State state)
    {
        if (state == State.Die && WillRevive)
        {
            State = State.Idle;
            Hp = (int)(MaxHp * ReviveHpRate);
            if (Targetable == false) Targetable = true;
            BroadcastHealth();
            BroadcastPos();
            // 부활 Effect 추가
        }
    }
    
    protected virtual void SetDirection()
    {
        if (Room == null) return;
        if (Target == null)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Stat.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        double deltaX = Target.CellPos.X - CellPos.X;
        double deltaZ = Target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        BroadcastPos();
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