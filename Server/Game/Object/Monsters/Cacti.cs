using Google.Protobuf.Protocol;

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
                    MaxHp += 40;
                    Hp += 40;
                    break;
                case Skill.CactiDefence2:
                    Defence += 2;
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