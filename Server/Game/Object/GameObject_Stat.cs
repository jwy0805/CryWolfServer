namespace Server.Game;

public partial class GameObject
{
    public int TotalAttack => Math.Max(Attack + AttackParam, 0);
    public float TotalAttackSpeed => Math.Max(AttackSpeed + AttackSpeedParam, 0);
    public int TotalSkillDamage => Math.Max(SkillDamage + SkillParam, 0);
    public int TotalDefence => Math.Max(Defence + DefenceParam, 0);
    public int TotalFireResist => Math.Max(FireResist + FireResistParam, 0);
    public int TotalPoisonResist => Math.Max(PoisonResist + PoisonResistParam, 0);
    public float TotalMoveSpeed => Math.Max(MoveSpeed + MoveSpeedParam, 0);
    public float TotalAttackRange => Math.Max(AttackRange + AttackRangeParam, 0);
    public float TotalSkillRange => Math.Max(SkillRange + SkillRangeParam, 0);
    public int TotalAccuracy => Math.Max(Accuracy + AccuracyParam, 0);
    public int TotalEvasion => Math.Max(Evasion + EvasionParam, 0);
    
    public int MpRecovery { get; set; } = 10;

    public virtual int Hp
    {
        get => Stat.Hp;
        set => Stat.Hp = Math.Clamp(value, 0, Stat.MaxHp);
    }

    public int MaxHp
    {
        get => Stat.MaxHp;
        set => Stat.MaxHp = value;
    }

    public int Mp
    {
        get => Stat.Mp;
        set => Stat.Mp = Math.Clamp(value, 0, Stat.MaxMp);
    }

    public int MaxMp
    {
        get => Stat.MaxMp;
        set => Stat.MaxMp = value;
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