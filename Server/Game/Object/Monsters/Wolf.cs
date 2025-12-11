using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Wolf : WolfPup
{
    private bool _drain = false;
    private bool _magicalAttack = false;
    protected readonly float DrainParam = DataManager.SkillDict[(int)Skill.WolfDrain].Value * 0.01f;
    
    public bool LastHitByWolf { get; set; } = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfHealth:
                    MaxHp += (int)DataManager.SkillDict[(int)Skill].Value;
                    Hp += (int)DataManager.SkillDict[(int)Skill].Value;
                    BroadcastHp();
                    break;
                case Skill.WolfMagicalAttack:
                    _magicalAttack = true;
                    break;
                case Skill.WolfDrain:
                    _drain = true;
                    break;
                case Skill.WolfCritical:
                    CriticalChance += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.WolfLastHitDna:
                    LastHitByWolf = true;
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
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
            Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.WolfSkill, SoundType = SoundType.D3 });
        }
        else
        {
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
            Room.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.WolfBite, SoundType = SoundType.D3 });
        }
        
        
        // Drain
        if (_drain)
        {
            var damage = _magicalAttack 
                ? Math.Max(TotalAttack - target.TotalDefence, 0) + Math.Max(TotalSkillDamage - target.TotalDefence, 0) 
                : Math.Max(TotalAttack - target.TotalDefence, 0);
            Hp += (int)(damage * DrainParam);
            BroadcastHp();
        }
    }
}