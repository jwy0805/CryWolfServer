using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Monster : GameObject
{
    public int MonsterNo;
    private const int CallCycle = 200;

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
        Console.WriteLine($"{target.CellPos.X}, {target.CellPos.Z}");
        Console.WriteLine();
        
        (Path, Atan) = Room.Map.Move(this, CellPos, _target.CellPos);
        State = State.Moving;

        for (int i = 0; i < Path.Count; i++)
        {
            Console.Write($"{Path[i].X}, {Path[i].Z} -> ");
        }
        Console.WriteLine();
    }

    private int _len;
    protected virtual void UpdateMoving()
    {
        // Targeting
        int timeNow = Room!.Stopwatch.Elapsed.Milliseconds;
        GameObject? target = null;
        if (timeNow > _lastSearch + SearchTick)
        {
            _lastSearch = timeNow;
            target = Room?.FindTarget(this);
        }

        if (_target?.Id != target?.Id)
        {
            _target = target;
            Vector3 destPos = Room!.Map.GetClosestPoint(CellPos, target!);
            (Path, Atan) = Room!.Map.Move(this, CellPos, destPos);
            _len = 0;
        }
        
        if (_target == null || _target.Room != Room)
        {
            _target = null;
            State = State.Idle;
            BroadcastMove();
            return;
        }

        // target이랑 너무 가까운 경우
        // Attack
        StatInfo targetStat = _target.Stat;
        Vector3 position = CellPos;
        if (targetStat.Targetable)
        {
            Vector3 targetCollider = Room!.Map.GetClosestPoint(CellPos, _target);
            float distance = new Vector3().SqrMagnitude(targetCollider - CellPos);
            if (distance <= AttackRange)
            {
                CellPos = position;
                State = State.Attack;
                BroadcastMove();
            }
        }

        // if (Path.Count - _len < Stat.SizeX + _target.Stat.SizeX)
        // {
        //     _len = 0;
        //     State = State.Idle;
        //     BroadcastMove();
        //     return;
        // }
        
        if (Room != null)
        {
            // path에 object가 있어서 갈 수 없는 경우
            if (Room.Map.CanGoGround(Path[_len]) == false)
            {
                State = State.Idle;
                BroadcastMove();
            }
            
            // 이동
            int cost = 0;
            while (CallCycle * MoveSpeed > cost * 25)
            {
                Room.Map.ApplyLeave(this);
                if (Path[_len].Z - CellPos.Z == 0 || Path[_len].X - CellPos.X == 0) cost += 10;
                else cost += 14;

                CellPos = Path[_len];
                Dir = (float)Atan[_len];
                if (_len >= Path.Count - 1)
                {
                    State = State.Idle;
                    break;
                }
                _len++;
                
                Room.Map.ApplyMap(this);
                // if (Math.Abs(Atan[_len - 1] - Atan[_len]) != 0) BroadcastMove();
            }

            BroadcastMove();
        }
    }

    private void BroadcastMove()
    {
        S_Move movePacket = new() { ObjectId = Id, PosInfo = PosInfo };
        Console.WriteLine($"{movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosZ}");
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