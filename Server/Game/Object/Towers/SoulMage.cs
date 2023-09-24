using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SoulMage : Haunt
{
    public bool Fire = false;
    private bool _defenceAll = false;
    private bool _tornado = false;
    private bool _shareDamage = false;
    private bool _natureAttack = false;
    private bool _debuffResist = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SoulMageAvoid:
                    Evasion += 20;
                    break;
                case Skill.SoulMageDefenceAll:
                    break;
                case Skill.SoulMageFireDamage:
                    Fire = true;
                    break;
                case Skill.SoulMageTornado:
                    _tornado = true;
                    break;
                case Skill.SoulMageShareDamage:
                    break;
                case Skill.SoulMageNatureAttack:
                    _natureAttack = true;
                    break;
                case Skill.SoulMageDebuffResist:
                    _debuffResist = true;
                    break;
                case Skill.SoulMageCritical:
                    CriticalChance += 25;
                    CriticalMultiplier = 1.5f;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        base.Update();
        if (Room == null) return;
        if (Room.Stopwatch.ElapsedMilliseconds > Time + MpTime)
        {
            Time = Room!.Stopwatch.ElapsedMilliseconds;
            Mp += Stat.MpRecovery;
        }

        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastMove();
            UpdateSkill();
            Mp = 0;
        }
        else
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
    }

    public override void RunSkill()
    {
        
    }

    public override void OnDamaged(GameObject attacker, int damage, bool reflected = false)
    {
        if (Room == null) return;
        
        damage = attacker.CriticalChance > 0 
            ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
            : Math.Max(damage - TotalDefence, 0);
        Hp = Math.Max(Stat.Hp - damage, 0);
        
        if (Reflection == true && reflected == false)
        {
            int refParam = (int)(damage * ReflectionRate);
            attacker.OnDamaged(this, refParam, true);
        }

        S_ChangeHp hpPacket = new S_ChangeHp { ObjectId = Id, Hp = Hp };
        Room.Broadcast(hpPacket);
        if (Hp <= 0) OnDead(attacker);
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
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    State = _tornado == true ? State.Skill : State.Attack;
                    SetDirection();
                }
                else
                {
                    State = State.Idle;
                }
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