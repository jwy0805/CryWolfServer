using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class Horror : Creeper
{
    private bool _poisonImmunity = false;
    private bool _rollPoison = false;
    private bool _poisonBelt = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.HorrorPoisonBelt:
                    _poisonBelt = true;
                    break;
                case Skill.HorrorPoisonImmunity:
                    _poisonImmunity = true;
                    break;
                case Skill.HorrorRollPoison:
                    _rollPoison = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (Mp >= MaxMp && _poisonBelt)
        {
            Effect poisonBelt = ObjectManager.Instance.CreateEffect(EffectId.PoisonBelt);
            poisonBelt.Room = Room;
            poisonBelt.Parent = this;
            poisonBelt.PosInfo = PosInfo;
            poisonBelt.Info.PosInfo = Info.PosInfo;
            poisonBelt.Info.Name = EffectId.PoisonBelt.ToString();
            poisonBelt.Init();
            Room.EnterGameParent(poisonBelt, this);
            Mp = 0;
        }
        
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
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }   
    }

    protected override void UpdateMoving()
    {
        if (Start == false)
        {
            Start = true;
            MoveSpeedParam += 2;
            State = State.Rush;
            BroadcastPos();
            return;
        }
        
        if (Start && SpeedRestore == false)
        {
            MoveSpeedParam -= 2;
            SpeedRestore = true;
        }
        
        base.UpdateMoving();
    }

    protected override void SetRollEffect(GameObject target)
    {
        target.OnDamaged(this, TotalSkillDamage, Damage.Normal);
        
        if (!_rollPoison) return; 
        // RollPoison Effect
        var effect = Room.EnterEffect(EffectId.HorrorRoll, this);
        Room.EnterGameParent(effect, effect.Parent ?? this);
        BuffManager.Instance.AddBuff(BuffId.DeadlyAddicted, Target, this, 0.05f, 5000);
    }

    public override void OnDamaged(GameObject attacker, int damage, Damage damageType, bool reflected = false)
    {
        if (Room == null) return;
        if (Invincible) return;
        if (damageType is Damage.Poison && _poisonImmunity) return;

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
        if (Hp <= 0) OnDead(attacker);
    }
}