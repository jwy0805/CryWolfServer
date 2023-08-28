using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Shell : Monster
{
    private bool _moveSpeedBuff = false;
    private bool _attackSpeedBuff = false;
    private bool _roll = false;
    private bool _start = false;
    private float _crashTime;
    
    protected readonly float MoveSpeedParam = 1f;
    protected readonly float AttackSpeedParam = 0.1f;
    protected readonly long RollCoolTime = 3000;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.ShellHealth:
                    MaxHp += 35;
                    Hp += 35;
                    break;
                case Skill.ShellSpeed:
                    _moveSpeedBuff = true;
                    break;
                case Skill.ShellAttackSpeed:
                    _attackSpeedBuff = true;
                    break;
                case Skill.ShellRoll:
                    _roll = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.Shell;
        _crashTime = 0f;
    }

    protected override void UpdateIdle()
    {
        if (Room!.Stopwatch.ElapsedMilliseconds < _crashTime + RollCoolTime && _start) return;
        _start = true;
        base.UpdateIdle();
    }
    
    protected override void UpdateMoving()
    {
        if (_roll)
        {
            State = State.Rush;
            BroadcastMove();
        }
        else
        {
            // Targeting
            double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
            if (timeNow > LastSearch + SearchTick)
            {
                LastSearch = timeNow;
                GameObject? target = Room?.FindNearestTarget(this);
                if (Target?.Id != target?.Id)
                {
                    Target = target;
                    if (Target != null)
                    {
                        DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                        (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                        BroadcastDest();
                    }
                }
            }
        
            if (Target == null || Target.Room != Room)
            {
                State = State.Idle;
                BroadcastMove();           
                return;
            }

            if (Room != null)
            {
                // 이동
                // target이랑 너무 가까운 경우
                // Attack
                StatInfo targetStat = Target.Stat;
                Vector3 position = CellPos;
                if (targetStat.Targetable)
                {
                    float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                    double deltaX = DestPos.X - CellPos.X;
                    double deltaZ = DestPos.Z - CellPos.Z;
                    Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                    if (distance <= AttackRange)
                    {
                        CellPos = position;
                        State = State.Idle;
                        BroadcastMove();
                        return;
                    }
                }
            }
            
            BroadcastMove();
        }
    }

    protected override void UpdateRush()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindNearestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Room != null)
        {
            // 이동
            // target이랑 너무 가까운 경우
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                // Roll 충돌 처리
                if (distance <= Stat.SizeX * 0.25 + 0.75f)
                {
                    CellPos = position;
                    _crashTime = Room.Stopwatch.ElapsedMilliseconds;
                    Target.OnDamaged(this, SkillDamage);
                    Mp += MpRecovery;
                    State = State.KnockBack;
                    DestPos = CellPos + (-Vector3.Normalize(Target.CellPos - CellPos) * 3);
                    BroadcastMove();
                    Room.Broadcast(new S_SetKnockBack
                    {
                        ObjectId = Id, 
                        Dest = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z }
                    });
                    return;
                }
            }
        }

        BroadcastMove();
    }

    protected override void UpdateKnockBack()
    {
        // 넉백중 충돌하면 Idle
        //
    }

    protected override void UpdateSkill()
    {
        State = State.Skill;
        BroadcastMove();
    }

    public override void RunSkill()
    {
        if (Room?.FindBuffTarget(this, GameObjectType.Monster) is not Creature creature) return;
        if (_moveSpeedBuff) BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, creature, MoveSpeedParam);
        if (_attackSpeedBuff) BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, creature, AttackSpeedParam);
    }
    
    public override void SetNextState()
    {
        if (Room == null) return; 
        
        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
        }
        else
        {
            if (Target.Hp > 0)
            {
                Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
                if (_roll) State = State.Rush;
                else State = distance <= AttackRange ? State.Idle : State.Moving;
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}