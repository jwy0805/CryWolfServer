using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class SnowBomb : Bomb
{
    private bool _areaAttack = false;
    private bool _burn = false;
    private bool _adjacentDamage = false;
    private int _readyToExplode = 0;

    protected float ExplosionAnimTime;
    protected readonly float ExplosionRange = 1.5f;
    protected GameObject Attacker;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnowBombFireResist:
                    FireResist += 20;
                    break;
                case Skill.SnowBombAreaAttack:
                    _areaAttack = true;
                    break;
                case Skill.SnowBombBurn:
                    _burn = true;
                    break;
                case Skill.SnowBombAdjacentDamage:
                    _adjacentDamage = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        Player.SkillUpgradedList.Add(Skill.SnowBombAreaAttack);
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 15;
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
            case State.Explode:
                UpdateExplode();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }
    }

    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        // Target이 사정거리 안에 있다가 밖으로 나간 경우 애니메이션 시간 고려하여 Attack 상태로 변경되도록 조정
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long animPlayTime = (long)(StdAnimTime / TotalAttackSpeed);
        if (distance <= TotalAttackRange)
        {
            if (LastAnimEndTime != 0 && timeNow <= LastAnimEndTime + animPlayTime) return;
            State = Mp >= MaxMp ? State.Skill : State.Attack;
            if (State == State.Skill) Mp = 0;
            SetDirection();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }

    
    protected virtual void UpdateExplode()
    {
        
    }

    protected override async void SkillImpactEvents(long impactTime)
    {
        if (Target == null || Room == null || Hp <= 0) return;
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Room == null || Hp <= 0) return;
            Room.SpawnProjectile(ProjectileId.SnowBombSkill, this, 5f);
        });
    }

    public override void ApplyEffectEffect()
    {
        var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        var gameObjects = Room.FindTargets(this, targetList, SkillRange);
        foreach (var gameObject in gameObjects)
        {
            gameObject.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            if (!_burn) continue;
            BuffManager.Instance.AddBuff(BuffId.Burn, gameObject, this, 0, 5000);
            OnExplode(Attacker);
        }
    }

    public virtual void ApplyProjectileEffect(GameObject? target, ProjectileId pid, PositionInfo posInfo)
    {
        if (Room == null || Hp <= 0) return;
        if (pid == ProjectileId.BombProjectile)
        {
            target?.OnDamaged(this, TotalAttack, Damage.Normal);
        }
        else
        {
            if (_areaAttack)
            {
                Room.SpawnEffect(EffectId.SnowBombExplosion, this, posInfo);
                var targetList = new[] { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
                var cellPos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
                var gameObjects = Room.FindTargets(cellPos, targetList, ExplosionRange);
                foreach (var gameObject in gameObjects)
                {
                    gameObject.OnDamaged(this, TotalSkillDamage, Damage.Normal);
                }
            }
            else
            {
                target?.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            }
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
        
        State =  Mp >= MaxMp ? State.Skill : State.Attack;
        if (State == State.Skill) Mp = 0;
        SetDirection();
    }

    public override void SetNextState(State state)
    {
        base.SetNextState(state);
        
        if (state == State.GoingToExplode && _readyToExplode > 2)
        {
            State = State.Explode;
            BroadcastPos();
        }
        else
        {
            _readyToExplode++;
        }
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;

        int totalDamage;
        if (damageType is Damage.Normal or Damage.Magical)
        {
            totalDamage = attacker.CriticalChance > 0 
                ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                : Math.Max(damage - TotalDefence, 0);
            if (damageType is Damage.Normal && Reflection && reflected == false)
            {
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp > 0) return;
        if (_adjacentDamage) OnGoingToExplode(attacker);
        else OnDead(attacker);
    }

    protected virtual void OnGoingToExplode(GameObject attacker)
    {
        if (Room == null) return;
        Targetable = false;
        Attacker = attacker;
        if (attacker.Target != null)
        {
            if (attacker.ObjectType is GameObjectType.Effect or GameObjectType.Projectile)
            {
                if (attacker.Parent != null)
                {
                    attacker.Parent.Target = null;
                    attacker.State = State.Idle;
                    BroadcastPos();
                }
            }
            attacker.Target = null;
            attacker.State = State.Idle;
            BroadcastPos();
        }

        State = State.GoingToExplode;
        BroadcastPos();
    }

    public virtual void OnExplode(GameObject attacker)
    {
        S_Die diePacket = new() { ObjectId = Id, AttackerId = attacker.Id };
        Room.Broadcast(diePacket);
        Room.DieAndLeave(Id);
    }
}