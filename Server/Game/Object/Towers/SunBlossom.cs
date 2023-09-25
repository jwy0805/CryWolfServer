using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunBlossom : Tower
{
    protected int HealParam = 40;
    protected int HealthParam = 50;
    protected float SlowParam = 0.2f;
    protected float SlowAttackParam = 0.2f;
    
    private bool _heal = false;
    private bool _health = false;
    private bool _slow = false;
    private bool _slowAttack = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunBlossomHeal:
                    _heal = true;
                    break;
                case Skill.SunBlossomHealth:
                    _health = true;
                    break;
                case Skill.SunBlossomSlow:
                    _slow = true;
                    break;
                case Skill.SunBlossomSlowAttack:
                    _slowAttack = true;
                    break;
            }
        }
    }

    public override void RunSkill()
    {
        if (Room?.FindBuffTarget(this, GameObjectType.Tower) is not Creature tower) return;
        if (Room.FindBuffTarget(this, GameObjectType.Monster) is not Creature monster) return;
        if (_heal) tower.Hp += HealParam;
        if (_health) BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, HealthParam);
        if (_slow) BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, SlowParam);
        if (_slowAttack) BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, SlowAttackParam);
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        State = State.Idle;
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}