using Google.Protobuf.Protocol;

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
                    MoveSpeed += 1;
                    break;
                case Skill.WolfPupAttack:
                    Attack += 4;
                    break;
                case Skill.WolfPupDefence:
                    Defence += 2;
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