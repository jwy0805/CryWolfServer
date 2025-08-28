using Google.Protobuf.Protocol;

namespace Server.Game;

public class PracticeDummy : Tower
{
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.PracticeDummyHealth:
                    MaxHp += 40;
                    Hp += 40;
                    break;
                case Skill.PracticeDummyHealth2:
                    MaxHp += 60;
                    Hp += 60;
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
        Room?.Broadcast(new S_PlaySound { ObjectId = Id, Sound = Sounds.DummyBlow, SoundType = SoundType.D3 });
    }
}