using Google.Protobuf.Protocol;
using Server.Game.Enchants;

namespace Server.Game;

public partial class GameObject
{
    private int _shieldAdd;
    private int _shieldRemain;

    public int TotalAttack
    {
        get
        {
            var attack = Math.Max(Attack + AttackParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.Attack, attack) ?? attack;
        }
    }

    public float TotalAttackSpeed
    {
        get
        {
            var attackSpeed = Math.Max(AttackSpeed + AttackSpeedParam, 0);
            return Room?.Enchant?.GetModifier(Player, StatType.AttackSpeed, attackSpeed) ?? attackSpeed;
        }
    }

    public int TotalSkillDamage
    {
        get
        {
            var skillDamage = Math.Max(SkillDamage + SkillParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.Skill, skillDamage) ?? skillDamage;
        }
    }

    public int TotalDefence
    {
        get
        {
            var defence = Math.Max(Defence + DefenceParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.Defence, defence) ?? defence;
        }
    }

    public int TotalFireResist
    {
        get
        {
            var fireResist = Math.Max(FireResist + FireResistParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.FireResist, fireResist) ?? fireResist;
        }
    }

    public int TotalPoisonResist
    {
        get
        {
            var poisonResist = Math.Max(PoisonResist + PoisonResistParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.PoisonResist, poisonResist) ?? poisonResist;
        }
    }

    public float TotalMoveSpeed
    {
        get
        {
            var moveSpeed = Math.Max(MoveSpeed + MoveSpeedParam, 0);
            return Room?.Enchant?.GetModifier(Player, StatType.MoveSpeed, moveSpeed) ?? moveSpeed;
        }
    }

    public float TotalAttackRange
    {
        get
        {
            var attackRange = Math.Max(AttackRange + AttackRangeParam, 0);
            return Room?.Enchant?.GetModifier(Player, StatType.AttackRange, attackRange) ?? attackRange;
        }
    }

    public float TotalSkillRange
    {
        get
        {
            var skillRange = Math.Max(SkillRange + SkillRangeParam, 0);
            return Room?.Enchant?.GetModifier(Player, StatType.SkillRange, skillRange) ?? skillRange;
        }
    }

    public int TotalAccuracy
    {
        get
        {
            var accuracy = Math.Max(Accuracy + AccuracyParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.Accuracy, accuracy) ?? accuracy;
        }
    }

    public int TotalEvasion
    {
        get
        {
            var evasion = Math.Max(Evasion + EvasionParam, 0);
            return (int?)Room?.Enchant?.GetModifier(Player, StatType.Evasion, evasion) ?? evasion;
        }
    }
    
    public int MpRecovery { get; set; } = 10;

    public virtual int Hp
    {
        get => Stat.Hp;
        set 
        {
            Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
            BroadcastHp(); 
        }
    }

    public int MaxHp
    {
        get => Stat.MaxHp;
        set
        {
            Stat.MaxHp = value;
            BroadcastHp();
        }
    }
    
    public int ShieldAdd
    {   
        // 더해지는 쉴드 양(쉴드 추가시 사용) - 쉴드에는 방어, 독 저항 등 적용되지 않으며 반사 또한 적용 x
        get => _shieldAdd;
        set
        {
            _shieldAdd += value;
            _shieldRemain += value;
            BroadcastShield();
        } 
    }

    public int ShieldRemain
    {   
        // 남아있는 쉴드 양(데미지를 받아 쉴드가 깎일 때 사용)
        get => _shieldRemain;
        set
        {
            _shieldRemain = value;
            if (_shieldRemain <= 0) _shieldAdd = 0;
            BroadcastShield();
        }
    }

    public int Mp
    {
        get => Stat.Mp;
        set
        {
            Stat.Mp = Math.Clamp(value, 0, Stat.MaxMp);
            BroadcastMp();
        }
    }

    public int MaxMp
    {
        get => Stat.MaxMp;
        set
        {
            Stat.MaxMp = value;
            BroadcastMp();
        }
    }
    
    public int Attack
    {
        get => Stat.Attack;
        set => Stat.Attack = value;
    }

    public int SkillDamage
    {
        get => Stat.Skill;
        set => Stat.Skill = value;
    }

    public int Defence
    {
        get => Stat.Defence;
        set => Stat.Defence = value;
    }

    public int FireResist
    {
        get => Stat.FireResist;
        set => Stat.FireResist = value;
    }

    public int PoisonResist
    {
        get => Stat.PoisonResist;
        set => Stat.PoisonResist = value;
    }

    public float MoveSpeed
    {
        get => Stat.MoveSpeed;
        set => Stat.MoveSpeed = value;
    }

    public float AttackSpeed
    {
        get => Stat.AttackSpeed;
        set => Stat.AttackSpeed = value;
    }

    public float AttackRange
    {
        get => Stat.AttackRange;
        set => Stat.AttackRange = value;
    }

    public float SkillRange
    {
        get => Stat.SkillRange;
        set => Stat.SkillRange = value;
    }
    
    public int CriticalChance
    {
        get => Stat.CriticalChance;
        set => Stat.CriticalChance = value;
    }

    public float CriticalMultiplier
    {
        get => Stat.CriticalMultiplier;
        set => Stat.CriticalMultiplier = value;
    }

    public int Accuracy
    {
        get => Stat.Accuracy;
        set => Stat.Accuracy = value;
    }

    public int Evasion
    {
        get => Stat.Evasion;
        set => Stat.Evasion = value;
    }

    public bool Targetable
    {
        get => Stat.Targetable; 
        set => Stat.Targetable = value;
    }

    public bool Aggro
    {
        get => Stat.Aggro;
        set => Stat.Aggro = value;
    }

    public bool Reflection
    {
        get => Stat.Reflection;
        set => Stat.Reflection = value;
    }

    public bool ReflectionSkill
    {
        get => Stat.ReflectionSkill;
        set => Stat.ReflectionSkill = value;
    }

    public float ReflectionRate
    {
        get => Stat.ReflectionRate;
        set => Stat.ReflectionRate = value;
    }

    public int UnitType
    {
        get => Stat.UnitType;
        set => Stat.UnitType = value;
    }

    public int AttackType
    {
        get => Stat.AttackType;
        set => Stat.AttackType = value;
    }
    
    public int Resource
    {
        get => Stat.Resource;
        set => Stat.Resource = value;
    }
    
    public int AttackParam { get; set; }
    public float AttackSpeedParam { get; set; }
    public int SkillParam { get; set; }
    public int DefenceParam { get; set; }
    public int FireResistParam { get; set; }
    public int PoisonResistParam { get; set; }
    public float MoveSpeedParam { get; set; }
    public int AttackRangeParam { get; set; }
    public int SkillRangeParam { get; set; }
    public int AccuracyParam { get; set; }
    public int EvasionParam { get; set; }
}