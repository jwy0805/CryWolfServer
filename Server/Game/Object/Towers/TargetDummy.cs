using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class TargetDummy : PracticeDummy
{
    private bool _heal = false;
    protected float HealParam = 0.15f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.TargetDummyHealSelf:
                    _heal = true;
                    break;
                case Skill.TargetDummyPoisonResist:
                    PoisonResist += 10;
                    break;
                case Skill.TargetDummyAggro:
                    Reflection = true;
                    ReflectionRate = 0.1f;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime && State != State.Die)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += 5;
            if (Mp >= MaxMp && _heal)
            {
                State = State.Skill;
                Mp = 0;
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
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || Hp <= 0) return;
            AttackEnded = true;
            Room.SpawnEffect(EffectId.StateHeal, this, this, PosInfo, true);
            Hp += (int)(MaxHp * HealParam);
        });
    }
}