using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Shell : Monster
{
    private bool _moveSpeedBuff = false;
    private bool _attackSpeedBuff = false;
    private bool _roll = false;
    private readonly float _moveSpeedParam = 0.1f;
    private readonly float _attackSpeedParam = 0.1f;
    
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

    protected override void UpdateAttack()
    {
        State = State.Idle;
        BroadcastMove();
    }

    protected override void UpdateSkill()
    {
        State = State.Skill;
        BroadcastMove();
    }

    public override void RunSkill()
    {
        if (Room?.FindBuffTarget(this, GameObjectType.Monster) is not Creature creature) return;
        if (_attackSpeedBuff) BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, creature, _attackSpeedParam);
        if (_moveSpeedBuff) BuffManager.Instance.AddBuff(BuffId.MoveSpeedIncrease, creature, _moveSpeedParam);
    }
}