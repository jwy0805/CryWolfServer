using Google.Protobuf.Protocol;

namespace Server.Game;

public class PracticeDummy : Tower
{
    private bool _aggro = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PracticeDummyDefence:
                    Defence += 3;
                    break;
                case Skill.PracticeDummyDefence2:
                    Defence += 4;
                    break;
                case Skill.PracticeDummyHealth:
                    MaxHp += 40;
                    Hp += 40;
                    break;
                case Skill.PracticeDummyHealth2:
                    MaxHp += 60;
                    Hp += 60;
                    break;
                case Skill.PracticeDummyAggro:
                    _aggro = true;
                    break;
            }
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

        if (_aggro == true && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastMove();
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
        List<GameObject> gameObjects = Room?.FindBuffTargets(this, GameObjectType.Monster, SkillRange)!;
        if (gameObjects.Count == 0) return;
        foreach (var creature in gameObjects.Cast<Creature>())
        {
            BuffManager.Instance.AddBuff(BuffId.Aggro, creature, this, 0, 3000);
        }
    }

    public override void SetNextState()
    {
        State = State.Idle;
        Room?.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}