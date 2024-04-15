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
            // switch (Skill)
            // {
            //     case Skill.TargetDummyHeal:
            //         _heal = true;
            //         break;
            //     case Skill.TargetDummyHealth:
            //         MaxHp += 100;
            //         Hp += 100;
            //         break;
            //     case Skill.TargetDummyFireResist:
            //         FireResist += 10;
            //         break;
            //     case Skill.TargetDummyPoisonResist:
            //         PoisonResist += 10;
            //         break;
            //     case Skill.TargetDummyReflection:
            //         Reflection = true;
            //         ReflectionRate = 0.1f;
            //         break;
            // }
        }
    }

    public override void Update()
    {
        if (Room != null) Job = Room.PushAfter(CallCycle, Update);
        if (Room == null) return;
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room!.Stopwatch.ElapsedMilliseconds;
            Mp += Stat.MpRecovery;
        }

        if (Mp >= MaxMp)
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

    public override void RunSkill()
    {
        base.RunSkill();
        if (_heal)
        {
            Hp += (int)(MaxHp * HealParam);
        }
    }
}