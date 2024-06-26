using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class TrainingDummy : TargetDummy
{
    private bool _faint = false;
    private readonly int _faintProb = 30;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.TrainingDummyAccuracy:
                    SkillRange += 4.0f;
                    break;
                case Skill.TrainingDummyHealth:
                    MaxHp += 200;
                    Hp += 200;
                    break;
                case Skill.TrainingDummyFaintAttack:
                    _faint = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
        Player.SkillSubject.SkillUpgraded(Skill.TrainingDummyAccuracy);
        Player.SkillSubject.SkillUpgraded(Skill.TrainingDummyHealth);
        Player.SkillSubject.SkillUpgraded(Skill.TrainingDummyFaintAttack);
    }

    protected override void AttackImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            if (_faint && new Random().Next(100) < _faintProb)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, Target, this, 0, 2500);
            }
            Target.OnDamaged(this, TotalAttack, Damage.Normal);
        });
    }
    
    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Target == null || Target.Targetable == false || Room == null || Hp <= 0) return;
            var targets = Room.FindTargets(
                this, new [] { GameObjectType.Tower }, TotalSkillRange);
            foreach (var target in targets)
            {
                if (target.Targetable == false || target.Hp <= 0) continue;
                
            }
        });
    }
}