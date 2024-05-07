using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Werewolf : Wolf
{
    private Random _rnd = new();
    private bool _thunder = false;
    private bool _berserker = false;
    private double _berserkerParam = 0;
    
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
                    CriticalMultiplier += 0.33f;
                    break;
                case Skill.WerewolfCriticalRate:
                    CriticalChance += 33;
                    break;
                case Skill.WerewolfBerserker:
                    _berserker = true;
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
            if (!_berserker) return;
            AttackParam -= Attack * (int)_berserkerParam;
            SkillParam -= Stat.Skill * (int)_berserkerParam;
            AttackSpeedParam -= AttackSpeed * (float)_berserkerParam;
                
            _berserkerParam = 0.5 * (MaxHp * 0.5 - Hp);
            AttackParam += Attack * (int)_berserkerParam;
            SkillParam += Stat.Skill * (int)_berserkerParam;
            AttackSpeedParam += AttackSpeed * (float)_berserkerParam;
        }
    }
    
    protected override void UpdateMoving()
    {
        // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target != null)
        {   
            // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
            Vector3 position = CellPos;
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                CellPos = position;
                State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
                BroadcastPos();
                return;
            }
            
            // Target이 있으면 이동
            DestPos = Room.Map.GetClosestPoint(CellPos, Target);
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos, false);
            BroadcastDest();
        }
        
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            BroadcastPos();
        }
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        Hp += (int)((Attack - target.Defence) * DrainParam);
        BroadcastHealth();
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));
        
        if (distance <= TotalAttackRange)
        {
            SetDirection();
            State = _thunder ? (_rnd.Next(2) == 0 ? State.Skill : State.Skill2) : State.Attack;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            DestPos = targetPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
    }
}