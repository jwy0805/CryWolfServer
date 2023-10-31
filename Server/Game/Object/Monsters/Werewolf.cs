using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Werewolf : Wolf
{
    private int _attackCount = 0;
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
                case Skill.WerewolfDebuffResist:
                    _debuffResist = true;
                    break;
                case Skill.WerewolfFaint:
                    _faint = true;
                    break;
                case Skill.WerewolfHealth:
                    MaxHp += 250;
                    Hp += 250;
                    BroadcastHealth();
                    break;
                case Skill.WerewolfEnhance:
                    _enhance = true;
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
                TotalAttack -= Attack * (int)_enhanceParam;
                TotalSkill -= Stat.Skill * (int)_enhanceParam;
                TotalAttackSpeed -= AttackSpeed * (float)(0.5 - (double)Stat.Hp / MaxHp);

                _enhanceParam = 0.5 * (MaxHp * 0.5 - Hp);
                TotalAttack += Attack * (int)_enhanceParam;
                TotalSkill += Stat.Skill * (int)_enhanceParam;
                TotalAttackSpeed += AttackSpeed * (float)(0.5 - (double)Stat.Hp / MaxHp);
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
            GameObject? target = Room?.FindNearestTarget(this);
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
                    if (_thunder)
                    {
                        State = _attackCount % 2 == 0 ? State.Skill : State.Skill2;
                        _attackCount++;
                    }
                    else State = State.Attack;
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
    
    protected override void UpdateDie()
    {
        _attackCount = 0;
        base.UpdateDie();
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        Hp += (int)((TotalAttack - target.TotalDefence) * DrainParam);
        if (_faint && Target != null) BuffManager.Instance.AddBuff(BuffId.Fainted, Target, this, 0, 1000);
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
                if (distance <= AttackRange)
                {
                    State = _thunder ? State.Skill : State.Attack;
                    SetDirection();
                }
                else State = State.Moving;
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