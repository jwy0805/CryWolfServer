using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class TargetDummy : PracticeDummy
{
    private bool _heal = false;
    private bool _aggro = false;
    protected float HealParam = DataManager.SkillDict[(int)Skill.TargetDummyHealSelf].Value;
    
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
                    PoisonResist += (int)DataManager.SkillDict[(int)Skill].Value;;
                    break;
                case Skill.TargetDummyAggro:
                    _aggro = true;
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
    
    public override void ApplyAttackEffect(GameObject target)
    {
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.DummyBlow, SoundType = SoundType.D3 });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || Hp <= 0) return;
            AttackEnded = true;
            Room.SpawnEffect(EffectId.StateHeal, this, this, PosInfo, true);
            Hp += (int)(MaxHp * HealParam);
            
            if (_aggro)
            {
                var targets = Room.FindTargets(this, 
                    new[] { GameObjectType.Monster }, TotalSkillRange);
                foreach (var target in targets)
                {
                    if (target.Targetable == false || target.Room != Room || target is not Monster monster) continue;
                    if (AddBuffAction == null) continue;
                    Room.Push(AddBuffAction, BuffId.Aggro, 
                        BuffParamType.None, monster, this, 0, 2000, false);
                }
            }
        });
    }
}