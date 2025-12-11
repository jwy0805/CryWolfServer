using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class WolfPup : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.WolfPupSpeed:
                    MoveSpeed += DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.WolfPupAttack:
                    Attack += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.WolfPupDefence:
                    Defence += (int)DataManager.SkillDict[(int)Skill].Value;
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
        Room?.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.WolfBite, SoundType = SoundType.D3 });
    }
}