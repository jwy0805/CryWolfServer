using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Cacti : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.CactiDefence:
                    MaxHp += (int)DataManager.SkillDict[(int)Skill].Value;
                    Hp += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.CactiFireResist:
                    FireResist += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Tanker;
    }
    
    public override void ApplyAttackEffect(GameObject target)
    {
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.MonsterAttack, SoundType = SoundType.D3 });
    }
}