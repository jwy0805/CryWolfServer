using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SunfloraPixie : SunflowerFairy
{

    private readonly float _attackSpeedParam = 0.15f;
    
    private bool _faint = false;
    private bool _curse = false;
    private bool _attackSpeedBuff = false;
    private bool _triple = false;
    private bool _debuffRemove = false;
    private bool _invincible = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SunfloraPixieFaint:
                    _faint = true;
                    break;
                case Skill.SunfloraPixieHeal:
                    HealParam = 75;
                    break;
                case Skill.SunfloraPixieRange:
                    AttackRange += 2;
                    SkillRange += 2;
                    break;
                case Skill.SunfloraPixieCurse:
                    _curse = true;
                    break;
                case Skill.SunfloraPixieAttackSpeed:
                    _attackSpeedBuff = true;
                    break;
                case Skill.SunfloraPixieTriple:
                    _triple = true;
                    break;
                case Skill.SunfloraPixieDebuffRemove:
                    _debuffRemove = true;
                    break;
                case Skill.SunfloraPixieAttack:
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)TowerId,
                        ObjectType = GameObjectType.Tower,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
                case Skill.SunfloraPixieInvincible:
                    _invincible = true;
                    break;
            }
        }
    }
    
    public override void RunSkill()
    {
        if (Room == null) return;
        
        List<Creature> towers = Room.FindBuffTargets(this, 
            new List<GameObjectType> { GameObjectType.Tower }, SkillRange).Cast<Creature>().ToList();
        if (towers.Count != 0)
        {
            foreach (var tower in towers)
            {
                tower.Hp += HealParam;
                Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, this, HealthParam);
                BuffManager.Instance.AddBuff(BuffId.AttackIncrease, tower, this, AttackParam);
                BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, tower, this, DefenceParam);
                if (_attackSpeedBuff) BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, tower, this, _attackSpeedParam);
                if (_debuffRemove) BuffManager.Instance.RemoveAllDebuff(tower);
            }
        }

        int num = _triple ? 3 : 2;
        if (_invincible)
        {
            foreach (var tower in towers)
                BuffManager.Instance.AddBuff(BuffId.Invincible, tower, this, 0, 3000);
        }
        
        List<Creature> monsters = Room.FindBuffTargets(this,
            new List<GameObjectType> { GameObjectType.Monster }, SkillRange).Cast<Creature>().ToList();
        if (monsters.Any())
        {
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())   
                BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, this, SlowParam);
            foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, this, SlowAttackParam);

            if (_faint)
            {
                foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                    BuffManager.Instance.AddBuff(BuffId.Fainted, monster, this, 0, 2000);
            }
            
            if (_curse)
            {
                foreach (var monster in monsters.OrderBy(_ => Guid.NewGuid()).Take(num).ToList())
                    BuffManager.Instance.AddBuff(BuffId.Curse, monster, this, 0, 5000);
            }
        }

        List<GameObject> fences = Room.FindBuffTargets(this, 
            new List<GameObjectType> { GameObjectType.Fence }, SkillRange);
        if (fences.Any())
        {
            foreach (var fence in fences)
            {
                fence.Hp += HealParam;
                Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
            }
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
        }
        else
        {
            if (Target.Hp > 0)
            {
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
                if (distance <= AttackRange)
                {
                    if (_faint)
                    {
                        State = State.Attack;
                        SetDirection();
                    }
                    else State = State.Idle;
                }
                else
                {
                    State = State.Idle;
                }
            }
            else
            {
                Target = null;
                State = State.Idle;
            }
        }
        
        Room.Broadcast(new S_State { ObjectId = Id, State = State });
    }
}