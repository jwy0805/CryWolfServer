using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Werewolf : Wolf
{
    private Random _rnd = new();
    private bool _thunder = false;
    private bool _debuffResist = false;
    private bool _faint = false;
    private bool _enhance = false;
    private double _enhanceParam = 0;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WerewolfThunder:
                    _thunder = true;
                    break;
                case Skill.WerewolfCriticalDamage:
                    break;
                case Skill.WerewolfCriticalRate:
                    break;
                case Skill.WerewolfBerserker:
                    break;
            }
        }
    }

    public override int Hp
    {
        get => Stat.Hp;
        set
        {
            Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
            if (_enhance)
            {
                // TotalAttack -= Attack * (int)_enhanceParam;
                // TotalSkill -= Stat.Skill * (int)_enhanceParam;
                // CriticalChance -= (int)_enhanceParam;
                // TotalAttackSpeed -= AttackSpeed * (float)_enhanceParam;
                //
                // _enhanceParam = 0.5 * (MaxHp * 0.5 - Hp);
                // TotalAttack += Attack * (int)_enhanceParam;
                // TotalSkill += Stat.Skill * (int)_enhanceParam;
                // CriticalChance += (int)_enhanceParam;
                // TotalAttackSpeed += AttackSpeed * (float)_enhanceParam;
            }
        }
    }
    
    protected override void UpdateMoving()
    {
        // Targeting
        double timeNow = Room!.Stopwatch.Elapsed.TotalMilliseconds;
        if (timeNow > LastSearch + SearchTick)
        {
            LastSearch = timeNow;
            GameObject? target = Room?.FindClosestTarget(this);
            if (Target?.Id != target?.Id)
            {
                Target = target;
                if (Target != null)
                {
                    DestPos = Room!.Map.GetClosestPoint(CellPos, Target);
                    (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
                    BroadcastDest();
                }
            }
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Room != null)
        {
            // 이동
            StatInfo targetStat = Target.Stat;
            Vector3 position = CellPos;
            if (targetStat.Targetable)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
                double deltaX = DestPos.X - CellPos.X;
                double deltaZ = DestPos.Z - CellPos.Z;
                Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
                // target이랑 너무 가까운 경우 Attack
                if (distance <= AttackRange)
                {
                    CellPos = position;
                    State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
                    BroadcastMove();
                    return;
                }
            }
            
            BroadcastMove();
        }
    }

    protected override void UpdateSkill()
    {
        UpdateAttack();
    }

    protected override void UpdateSkill2()
    {
        UpdateAttack();
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        Hp += (int)((Attack - target.Defence) * DrainParam);
        if (_faint && Target != null) BuffManager.Instance.AddBuff(BuffId.Fainted, Target, this, 0, 1000);
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastMove();
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
        
        if (distance > TotalAttackRange)
        {
            DestPos = targetPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
            SetDirection();
        }
    }
}