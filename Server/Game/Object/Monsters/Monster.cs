using Google.Protobuf.Protocol;

namespace Server.Game;

public class Monster : GameObject
{
    public int MonsterNo;
    
    public Monster()
    {
        ObjectType = GameObjectType.Monster;
    }

    public override void Update()
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

    private GameObject _target;
    private long _nextSearchTick = 0;
    protected virtual void UpdateIdle()
    {
        if (_nextSearchTick > Environment.TickCount64) return;
        _nextSearchTick = Environment.TickCount64 + 500;

        List<GameObjectType> targetList = new List<GameObjectType>
            { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        
        GameObject? target = Room.FindTarget(targetList, this);
        if (target == null) return;
        _target = target;
        State = State.Moving;
    }

    protected virtual void UpdateMoving()
    {
        
    }

    protected virtual void UpdateAttack()
    {
        
    }

    protected virtual void UpdateSkill()
    {
        
    }

    protected virtual void UpdateSkill2()
    {
        
    }

    protected virtual void UpdateDie()
    {
        
    }

    protected virtual void UpdateKnockBack()
    {
        
    }
    
    protected virtual void UpdateRush()
    {
        
    }
}