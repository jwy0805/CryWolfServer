using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class TrainingDummy : TargetDummy
{
    private bool _faint = false;
    private bool _debuffRemove = false;
    private readonly int _faintProb = 30;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.TrainingDummyAggro:
            //         SkillRange += 4.0f;
            //         break;
            //     case Skill.TrainingDummyDefence:
            //         Defence += 6;
            //         break;
            //     case Skill.TrainingDummyHeal:
            //         HealParam += 0.1f;
            //         break;
            //     case Skill.TrainingDummyHealth:
            //         MaxHp += 200;
            //         Hp += 200;
            //         BroadcastHealth();
            //         break;
            //     case Skill.TrainingDummyFireResist:
            //         FireResist += 15;
            //         break;
            //     case Skill.TrainingDummyPoisonResist:
            //         PoisonResist += 15;
            //         break;
            //     case Skill.TrainingDummyFaint:
            //         _faint = true;
            //         break;
            //     case Skill.TrainingDummyDebuffRemove:
            //         _debuffRemove = true;
            //         break;
            // }
        }
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        if (!_faint) return;
        Random r = new Random();
        if (r.Next(99) < _faintProb)
        {
            target.State = State.Faint;
            BroadcastPos();
        }
    }
    
    public override void RunSkill()
    {
        base.RunSkill();
        if (_debuffRemove == true) BuffManager.Instance.RemoveAllDebuff(this);
    }


}