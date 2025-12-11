using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class TrainingDummy : TargetDummy
{
    private bool _accuracy = false;
    private bool _faint = false;
    
    private readonly int _accuracyBuffParam = (int)DataManager.SkillDict[(int)Skill.TrainingDummyAccuracy].Value;
    private readonly int _faintProb = (int)DataManager.SkillDict[(int)Skill.TrainingDummyFaintAttack].Value;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.TrainingDummyFaintAttack:
                    _faint = true;
                    break;
                case Skill.TrainingDummyAccuracy:
                    _accuracy = true;
                    break;
                case Skill.TrainingDummyHealth:
                    MaxHp += (int)DataManager.SkillDict[(int)Skill].Value;
                    Hp += (int)DataManager.SkillDict[(int)Skill].Value;
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
            if (Mp >= MaxMp)
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
    
    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            AttackEnded = true;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            
            if (_faint && new Random().Next(100) < _faintProb)
            {
                Room.Push(AddBuffAction, BuffId.Fainted,
                    BuffParamType.None, Target, this, 0, 2500, false);
            }
            
            Room.Push(Target.OnDamaged, this, TotalAttack, Damage.Normal, false);
            Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.TrainingDummyBlow, SoundType = SoundType.D3 });
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            AttackEnded = true;
            
            // Heal, Aggro -> Inherited from TargetDummy
            Room.SpawnEffect(EffectId.StateHeal, this, this, PosInfo, true);
            Hp += (int)(MaxHp * HealParam);
            
            var aggroTargets = Room.FindTargets(this, 
                new[] { GameObjectType.Monster }, TotalSkillRange);
            foreach (var target in aggroTargets)
            {
                if (target.Targetable == false || target.Room != Room || target is not Monster monster) continue;
                if (AddBuffAction == null) continue;
                Room.Push(AddBuffAction, BuffId.Aggro, 
                    BuffParamType.None, monster, this, 0, 2000, false);
            }
            
            // Accuracy Buff
            if (_accuracy)
            {   
                var targets = Room.FindTargets(
                    this, new [] { GameObjectType.Tower }, TotalSkillRange);
                foreach (var target in targets)
                {
                    if (target.Targetable == false || target.Hp <= 0) continue;
                    Room.Push(AddBuffAction, BuffId.AccuracyBuff,
                        BuffParamType.Constant, target, this, _accuracyBuffParam, 5000, false);
                }
            }
        });
    }
}