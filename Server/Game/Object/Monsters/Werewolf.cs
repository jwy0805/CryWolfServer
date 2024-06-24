using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Werewolf : Wolf
{
    private bool _thunder;
    private bool _berserker;
    private float _berserkerParam;
    
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
            if (_berserker == false) return;
            AttackParam -= (int)(Attack * _berserkerParam);
            SkillParam -= (int)(Stat.Skill * _berserkerParam);
            AttackSpeedParam -= AttackSpeed * (float)_berserkerParam;
                
            _berserkerParam = (MaxHp - Hp) / (float)MaxHp;
            AttackParam += (int)(Attack * _berserkerParam);
            SkillParam += (int)(Stat.Skill * _berserkerParam);
            AttackSpeedParam += AttackSpeed * (float)_berserkerParam;
        }
    }

    public override void Init()
    {
        base.Init();
        AttackImpactMoment = 0.5f;
        SkillImpactMoment = 0.3f;
        DrainParam = 0.18f;
        
        Player.SkillSubject.SkillUpgraded(Skill.WerewolfThunder);
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
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);        
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance <= TotalAttackRange)
        {
            State = _thunder ? GetRandomState(State.Skill, State.Skill2) : State.Attack;
            SyncPosAndDir();
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void UpdateSkill()
    {
        if (IsAttacking) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            IsAttacking = false;
            return;
        }
        var packet = new S_SetAnimSpeed
        {
            ObjectId = Id,
            SpeedParam = TotalAttackSpeed
        };
        Room.Broadcast(packet);
        long timeNow = Room!.Stopwatch.ElapsedMilliseconds;
        long impactTime = (long)(StdAnimTime / TotalAttackSpeed * AttackImpactMoment);
        long animTime = (long)(StdAnimTime / TotalAttackSpeed);
        long nextAnim = LastAnimEndTime - timeNow + animTime;
        long nextImpact = LastAnimEndTime - timeNow + impactTime;
        long nextAnimEndTime = nextAnim > 0 ? Math.Min(nextAnim, animTime) : animTime;
        long nextImpactTime = nextImpact > 0 ? Math.Min(nextImpact, impactTime) : impactTime;
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
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            Room.SpawnEffect(EffectId.LightningStrike, this, Target.PosInfo);
            var damage = Math.Max(TotalSkillDamage - Target.TotalDefence, 0);
            Hp += (int)(damage * DrainParam);
            BroadcastHp();
            Target.OnDamaged(this, TotalSkillDamage, Damage.Magical);
            // TODO : DNA
        });
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        var magicalParam = (int)(TotalSkillDamage * 0.2f);
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0) 
                     + Math.Max(magicalParam - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
        Room?.SpawnEffect(EffectId.WerewolfMagicalEffect, this, target.PosInfo, true);
        
        target.OnDamaged(this, magicalParam, Damage.Magical);
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        // TODO : DNA
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
        }
        else
        {
            State = _thunder ? GetRandomState(State.Skill, State.Skill2) : State.Attack;
            SyncPosAndDir();
        }
    }
}