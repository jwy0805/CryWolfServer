using Google.Protobuf.Protocol;

namespace Server.Game;

public class Wolf : WolfPup
{
    private bool _drain = false;
    private bool _magicalAttack = false;
    protected float DrainParam = 0.12f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfHealth:
                    MaxHp += 80;
                    Hp += 80;
                    BroadcastHp();
                    break;
                case Skill.WolfMagicalAttack:
                    _magicalAttack = true;
                    break;
                case Skill.WolfDrain:
                    _drain = true;
                    break;
                case Skill.WolfCritical:
                    CriticalChance += 20;
                    break;
                case Skill.WolfLastHitDna:
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
    }

    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null) return;
        
        if (_magicalAttack)
        {
            Room.SpawnEffect(EffectId.WolfMagicalEffect, this, this, target.PosInfo, true);
            Room.Push(target.OnDamaged, this, TotalSkillDamage, Damage.Magical, false);
        }
        
        Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        
        // Drain
        if (_drain)
        {
            var damage = _magicalAttack 
                ? Math.Max(TotalAttack - target.TotalDefence, 0) + Math.Max(TotalSkillDamage - target.TotalDefence, 0) 
                : Math.Max(TotalAttack - target.TotalDefence, 0);
            Hp += (int)(damage * DrainParam);
            BroadcastHp();
        }

        // TODO : DNA
    }
}