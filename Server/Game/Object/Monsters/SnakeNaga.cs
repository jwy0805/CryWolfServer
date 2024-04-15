using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SnakeNaga : Snake
{
    private bool _drain = false;
    private bool _meteor = true;
    private readonly float _drainParam = 0.2f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            // switch (Skill)
            // {
            //     case Skill.SnakeNagaAttack:
            //         Attack += 10;
            //         break;
            //     case Skill.SnakeNagaRange:
            //         AttackRange += 2;
            //         break;
            //     case Skill.SnakeNagaCritical:
            //         CriticalChance += 25;
            //         break;
            //     case Skill.SnakeNagaFireResist:
            //         FireResist += 40;
            //         break;
            //     case Skill.SnakeNagaDrain:
            //         _drain = true;
            //         break;
            //     case Skill.SnakeNagaMeteor:
            //         _meteor = true;
            //         break;
            // }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Mp += Stat.MpRecovery;
        }

        if (MaxMp != 1 && Mp >= MaxMp && _meteor)
        {
            State = State.Skill;
            BroadcastPos();
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
    
    public override void SetNormalAttackEffect(GameObject target)
    {
        if (_drain) Hp += (int)((Attack - target.Defence) * _drainParam);
        BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 5f);
    }
}