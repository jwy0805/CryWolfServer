using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Monster : GameObject
{
    public int MonsterNo;
    
    public Monster()
    {
        ObjectType = GameObjectType.Monster;
    }

    public void Init()
    {
        DataManager.MonsterDict.TryGetValue(MonsterNo, out var monsterData);
        Stat.MergeFrom(monsterData!.stat);
        Stat.Hp = monsterData.stat.MaxHp;

        State = State.Idle;
    }
    
    public void Init(int monsterNo)
    {
        MonsterNo = monsterNo;
    
        DataManager.MonsterDict.TryGetValue(MonsterNo, out var monsterData);
        Stat.MergeFrom(monsterData!.stat);
        Stat.Hp = monsterData.stat.MaxHp;
    
        State = State.Idle;
    }

    private IJob _job;
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

        if (Room != null) _job = Room.PushAfter(200, Update);
    }

    private GameObject? _target;
    private long _nextSearchTick = 0;
    protected virtual void UpdateIdle()
    {
        if (_nextSearchTick > Environment.TickCount64) return;
        _nextSearchTick = Environment.TickCount64 + 500;
        
        Tags = new List<GameObjectType>
            { GameObjectType.Tower, GameObjectType.Fence, GameObjectType.Sheep };
        
        GameObject? target = Room?.FindTarget(Tags, this);
        if (target == null) return;
        _target = target;
        
        if (Room == null) return;
        (Path, Atan) = Room.Map.Move(this, CellPos, _target.CellPos);
        State = State.Moving;
    }

    private long _nextMoveTick = 0;
    private int _len = 1;
    protected virtual void UpdateMoving()
    {
        if (_nextMoveTick > Environment.TickCount64) return;

        if (_target == null || _target.Room != Room)
        {
            _target = null;
            State = State.Idle;
            BroadcastMove();
            return;
        }

        // target이랑 너무 가까운 경우
        if (Path.Count - _len < Math.Max(Stat.SizeX, Stat.SizeZ) + Math.Max(_target.Stat.SizeX, _target.Stat.SizeZ))
        {
            _len = 1;
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        if (Room != null)
        {
            // path에 object가 있어서 갈 수 없는 경우
            if (Room.Map.CanGoGround(Path[_len]) == false)
            {
                State = State.Idle;
                BroadcastMove();
            }
            
            // 이동
            int moveTick;
            if (Path[_len].Z - CellPos.Z == 0 || Path[_len].X - CellPos.X == 0) moveTick = (int)(1000 / (MoveSpeed * 4.0));
            else moveTick = (int)(1000 / (MoveSpeed * (4.0 / Math.Sqrt(2))));
        
            _nextMoveTick = Environment.TickCount64 + moveTick;
            CellPos = Path[_len];
            Dir = (float)Atan[_len];
            _len++;

            Room.Map.ApplyMap(this, CellPos);
            BroadcastMove();
        }
    }

    private void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Room?.Broadcast(movePacket);
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