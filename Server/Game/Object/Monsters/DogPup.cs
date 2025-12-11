using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class DogPup : Monster
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.DogPupSpeed:
                    MoveSpeed += DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.DogPupEvasion:
                    Evasion += (int)DataManager.SkillDict[(int)Skill].Value;
                    break;
                case Skill.DogPupAttackSpeed:
                    AttackSpeed += AttackSpeed * (int)(DataManager.SkillDict[(int)Skill].Value / (float)100);
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
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.DogBite, SoundType = SoundType.D3 });
    }
}