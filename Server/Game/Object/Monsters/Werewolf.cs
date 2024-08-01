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
        UnitRole = Role.Warrior;
        AttackImpactMoment = 0.5f;
        SkillImpactMoment = 0.3f;
        DrainParam = 0.18f;
        
        Player.SkillSubject.SkillUpgraded(Skill.WerewolfThunder);
    }
    
    protected override void UpdateMoving()
    {
        if (Room == null) return;
        
        // Targeting
        Target = Room.FindClosestTarget(this);
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

    protected override void UpdateSkill2()
    {
        UpdateSkill();
    }
    
    protected override async void SkillImpactEvents(long impactTime)
    {
        await Scheduler.ScheduleEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            AttackEnded = true;
            Room.SpawnEffect(EffectId.LightningStrike, this, Target.PosInfo);
            var damage = Math.Max(TotalSkillDamage - Target.TotalDefence, 0);
            Hp += (int)(damage * DrainParam);
            BroadcastHp();
            Room.Push(Target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
            // TODO : DNA
        });
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null) return;
        
        var magicalParam = (int)(TotalSkillDamage * 0.2f);
        var damage = Math.Max(TotalAttack - target.TotalDefence, 0) 
                     + Math.Max(magicalParam - target.TotalDefence, 0);
        Hp += (int)(damage * DrainParam);
        Room.SpawnEffect(EffectId.WerewolfMagicalEffect, this, target.PosInfo, true);
        Room.Push(target.OnDamaged, this, magicalParam, Damage.Magical, false);
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        // TODO : DNA
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
        }
        else
        {
            State = _thunder ? GetRandomState(State.Skill, State.Skill2) : State.Attack;
            SyncPosAndDir();
        }
    }
}