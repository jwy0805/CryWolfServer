using Google.Protobuf.Protocol;

namespace Server.Game;

public class Rabbit : Tower
{
    public bool _aggro = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.RabbitAggro:
                    _aggro = true;
                    break;
                case Skill.RabbitDefence:
                    Defence += 3;
                    break;
                case Skill.RabbitEvasion:
                    Evasion += 10;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (_aggro && Mp >= MaxMp)
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
    }

    public override void RunSkill()
    {
        if (Room == null) return;
        if (Target is not Creature) return;
        BuffManager.Instance.AddBuff(BuffId.Aggro, Target, this, 0, 2000);
    }
}