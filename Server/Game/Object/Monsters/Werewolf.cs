using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Werewolf : Wolf
{
    private readonly Random _rnd = new();
    private bool _thunder = false;
    private bool _berserker = false;
    private double _berserkerParam = 0;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WerewolfThunder:
                    _thunder = true;
                    break;
                case Skill.WerewolfCriticalDamage:
                    CriticalMultiplier += 0.33f;
                    break;
                case Skill.WerewolfCriticalRate:
                    CriticalChance += 33;
                    break;
                case Skill.WerewolfBerserker:
                    _berserker = true;
                    break;
            }
        }
    }

    public override int Hp
    {
        get => Stat.Hp;
        set
        {
            Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
            if (!_berserker) return;
            AttackParam -= Attack * (int)_berserkerParam;
            SkillParam -= Stat.Skill * (int)_berserkerParam;
            AttackSpeedParam -= AttackSpeed * (float)_berserkerParam;
                
            _berserkerParam = 0.5 * (MaxHp * 0.5 - Hp);
            AttackParam += Attack * (int)_berserkerParam;
            SkillParam += Stat.Skill * (int)_berserkerParam;
            AttackSpeedParam += AttackSpeed * (float)_berserkerParam;
        }
    }

    public override void Init()
    {
        base.Init();
        AttackImpactTime = 0.5f;
        SkillImpactTime = 0.3f;
    }

    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            BroadcastPos();
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance <= TotalAttackRange)
        {
            State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
            BroadcastPos();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void UpdateSkill()
    {
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
            return;
        }
        
        float dist = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
        if (dist > TotalAttackRange + 0.4f)
        {
            State = State.Idle;
            IsAttacking = false;
            return;
        }

        if (IsAttacking) return;
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactTime = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactTime);
        long animTime = (long)(StdAnimTime / TotalAttackSpeed);
        long nextAnimEndTime = StateChanged ? animTime : LastAttackTime - timeNow + animTime;
        long nextImpactTime = StateChanged ? impactTime : LastAttackTime - timeNow + impactTime;
        SkillImpactEvents(nextImpactTime);
        EndEvents(nextAnimEndTime);
        IsAttacking = true;
    }

    protected override void UpdateSkill2()
    {
        UpdateSkill();
    }
    
    protected override async void SkillImpactEvents(long impactTime)
    {
        if (Target == null) return;
        await Scheduler.ScheduleEvent(impactTime, () => 
            Room.SpawnEffect(EffectId.LightningStrike, this, PosInfo));
    }
    
    public override void ApplyNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        Hp += (int)((Attack - target.Defence) * DrainParam);
        BroadcastHealth();
    }
    
    public override void SetNextState()
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
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));

        if (distance <= TotalAttackRange)
        {
            State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
            SetDirection();
            IsAttacking = false;
        }
        else
        {
            State = State.Moving;
        }
    }
}