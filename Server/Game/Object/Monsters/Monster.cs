using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Monster : GameObject
{
    public int MonsterNo;
    private const int CallCycle = 200;
    protected Vector3 DestPos;
    protected Vector3 TempDest;

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

        if (Room != null) _job = Room.PushAfter(CallCycle, Update);
    }

    private GameObject? _target;
    private const int SearchTick = 800;
    private int _lastSearch = 0;
    protected virtual void UpdateIdle()
    {
        GameObject? target = Room?.FindTarget(this);
        if (target == null || Room == null) return;
        _lastSearch = Room!.Stopwatch.Elapsed.Milliseconds;
        _target = target;
        DestPos = Room!.Map.GetClosestPoint(CellPos, _target);
        Console.WriteLine($"{target.CellPos.X}, {target.CellPos.Y}, {target.CellPos.Z}");
        Console.WriteLine();
        
        (Path, Atan) = Room.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        State = State.Moving;

        // for (int i = 0; i < Path.Count; i++)
        // {
        //     Console.Write($"{Path[i].X}, {Path[i].Z} -> ");
        // }
        // Console.WriteLine();
    }

    protected virtual void UpdateMoving()
    {
        // Targeting
        int timeNow = Room!.Stopwatch.Elapsed.Milliseconds;
        if (timeNow > _lastSearch + SearchTick)
        {
            _lastSearch = timeNow;
            GameObject? target = Room?.FindTarget(this);
            if (_target?.Id != target?.Id)
            {
                _target = target;
                if (_target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, _target);
                    (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (_target == null || _target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Room != null)
        {
            // // path에 object가 있어서 갈 수 없는 경우
            // if (Room.Map.CanGoGround(Path[_len]) == false)
            // {
            //     State = State.Idle;
            //     BroadcastMove();
            // }
            
            // 이동
            // target이랑 너무 가까운 경우
            // Attack
            StatInfo targetStat = _target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = State.Attack;
                    BroadcastMove();
                    return;
                }
            }

            BroadcastMove();
        }
    }

    protected virtual void UpdateAttack()
    {
        if (_target == null || _target.Room != Room || Room == null) return;
        if (_target.Stat.Targetable == false) return;

        double deltaX = _target.CellPos.X - CellPos.X;
        double deltaZ = _target.CellPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        BroadcastMove();
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