using Google.Protobuf.Protocol;

namespace Server.Game;

public class SkeletonGiant : Skeleton
{
    private bool _defenceDebuff = false;
    private bool _attackSteal = false;
    private bool _revive = false;
    private bool _alreadyRevived = false;
    protected readonly int DefenceDebuffParam = 3;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SkeletonGiantDefenceDebuff:
                    _defenceDebuff = true;
                    break;
                case Skill.SkeletonGiantAttackSteal:
                    _attackSteal = true;
                    break;
                case Skill.SkeletonGiantMpDown:
                    MaxMp -= 25;
                    break;
                case Skill.SkeletonGiantRevive:
                    _revive = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (MaxMp != 1 && Mp >= MaxMp)
        {
            State = State.Skill;
            BroadcastPos();
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
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Revive:
                    UpdateRevive();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }   
        }
    }

    private void UpdateRevive()
    {
        
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        target.DefenceParam -= DefenceDebuffParam;
        
    }

    public override void SetNextState(State state)
    {
        
    }
    
    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;

        int totalDamage;
        if (damageType is Damage.Normal or Damage.Magical)
        {
            totalDamage = attacker.CriticalChance > 0 
                ? Math.Max((int)(damage * attacker.CriticalMultiplier - TotalDefence), 0) 
                : Math.Max(damage - TotalDefence, 0);
            if (damageType is Damage.Normal && Reflection && reflected == false)
            {
                int refParam = (int)(totalDamage * ReflectionRate);
                attacker.OnDamaged(this, refParam, damageType, true);
            }
        }
        else
        {
            totalDamage = damage;
        }
        
        Hp = Math.Max(Hp - totalDamage, 0);
        var damagePacket = new S_GetDamage { ObjectId = Id, DamageType = damageType, Damage = totalDamage };
        Room.Broadcast(damagePacket);
        if (Hp > 0) return;
        if (_alreadyRevived == false && _revive)
        {
            _alreadyRevived = true;
            State = State.Revive;
            BroadcastPos();
            return;
        }
        OnDead(attacker);
    }
}