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
            // switch (Skill)
            // {
            //     case Skill.PracticeDummyDefence:
            //         Defence += 3;
            //         break;
            //     case Skill.PracticeDummyDefence2:
            //         Defence += 4;
            //         break;
            //     case Skill.PracticeDummyHealth:
            //         MaxHp += 40;
            //         Hp += 40;
            //         BroadcastHealth();
            //         break;
            //     case Skill.PracticeDummyHealth2:
            //         MaxHp += 60;
            //         Hp += 60;
            //         BroadcastHealth();
            //         break;
            //     case Skill.PracticeDummyAggro:
            //         _aggro = true;
            //         break;
            // }
        }
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

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

        // if (_aggro && Mp >= MaxMp)
        // {
        //     State = State.Skill;
        //     BroadcastMove();
        //     UpdateSkill();
        //     Mp = 0;
        // }
        // else
        // {
        //     switch (State)
        //     {
        //         case State.Die:
        //             UpdateDie();
        //             break;
        //         case State.Moving:
        //             UpdateMoving();
        //             break;
        //         case State.Idle:
        //             UpdateIdle();
        //             break;
        //         case State.Rush:
        //             UpdateRush();
        //             break;
        //         case State.Attack:
        //             UpdateAttack();
        //             break;
        //         case State.Skill:
        //             UpdateSkill();
        //             break;
        //         case State.Skill2:
        //             UpdateSkill2();
        //             break;
        //         case State.KnockBack:
        //             UpdateKnockBack();
        //             break;
        //         case State.Faint:
        //             break;
        //         case State.Standby:
        //             break;
        //     }
        // }
    }
    
    public override void RunSkill()
    {
        if (Room == null) return;
        
        List<GameObject> gameObjects = Room.FindTargets(this,
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange);
        if (!gameObjects.Any()) return;
        foreach (var creature in gameObjects.Cast<Creature>())
        {
            BuffManager.Instance.AddBuff(BuffId.Aggro, creature, this, 0, 3000);
        }
    }
}