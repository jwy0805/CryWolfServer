using Google.Protobuf.Protocol;

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
                    MoveSpeed += 0.5f;
                    break;
                case Skill.DogPupAttackSpeed:
                    AttackSpeed += AttackSpeed * 0.1f;
                    break;
                case Skill.DogPupEvasion:
                    Evasion += 10;
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