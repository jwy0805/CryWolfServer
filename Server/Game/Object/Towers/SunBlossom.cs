using Google.Protobuf.Protocol;

namespace Server.Game;

public class SunBlossom : Tower
{
    protected int HealParam = 40;
    protected readonly int HealthParam = 50;
    protected readonly float SlowParam = 0.2f;
    protected readonly float SlowAttackParam = 0.2f;
    
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

    protected override void UpdateIdle() { }

    public override void RunSkill()
    {
        if (Room == null) return;
        
        List<Creature> towers = Room.FindTargets(this, 
            new List<GameObjectType> { GameObjectType.Tower }, SkillRange).Cast<Creature>().ToList();
        if (towers.Any())
        {
            foreach (var tower in towers)
            {
                if (_heal)
                {
                    tower.Hp += HealParam;
                    Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                }
                if (_health) BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, this, HealthParam);
            }
        }

        List<Creature> monsters = Room.FindTargets(this,
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange).Cast<Creature>().ToList();
        if (monsters.Any())
        {
            if (_slow)
            {
                foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(1).ToList())
                    BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, this, SlowParam);
            }
            
            if (_slowAttack)
            {
                foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(1).ToList())
                    BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, this, SlowAttackParam);
            }
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        State = State.Idle;
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}