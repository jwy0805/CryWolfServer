using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SunfloraPixie : SunflowerFairy
{
    public bool Curse = false;

    private readonly float _attackSpeedParam = 0.15f;
    
    private bool _faint = false;
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
                    Curse = true;
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
        
        List<GameObject> towers = new List<GameObject>();
        List<GameObject> monsters = new List<GameObject>();
        if (_triple)
        {
            towers = Room.FindBuffTargets(this, GameObjectType.Tower, 3);
            monsters = Room.FindBuffTargets(this, GameObjectType.Monster, 3);
        }
        else
        {
            towers = Room.FindBuffTargets(this, GameObjectType.Tower, 2);
            monsters = Room.FindBuffTargets(this, GameObjectType.Monster, 2);
        }

        if (towers.Count != 0)
        {
            foreach (var tower in towers)
            {
                tower.Hp += HealParam;
                BuffManager.Instance.AddBuff(BuffId.HealthIncrease, tower, HealthParam);
                BuffManager.Instance.AddBuff(BuffId.AttackIncrease, tower, AttackParam);
                BuffManager.Instance.AddBuff(BuffId.DefenceIncrease, tower, DefenceParam);
                if (_attackSpeedBuff)
                    BuffManager.Instance.AddBuff(BuffId.AttackSpeedIncrease, tower, _attackSpeedParam);
                if (_debuffRemove) BuffManager.Instance.RemoveAllBuff((Creature)tower);
            }

            if (_invincible)
            {
                Random random = new Random();
                int randomIndex = random.Next(0, towers.Count);
                BuffManager.Instance.AddBuff(BuffId.Invincible, towers[randomIndex], 0, 3000);
            }
        }

        if (monsters.Count != 0)
        {
            foreach (var monster in monsters)
            {
                BuffManager.Instance.AddBuff(BuffId.MoveSpeedDecrease, monster, SlowParam);
                BuffManager.Instance.AddBuff(BuffId.AttackSpeedDecrease, monster, SlowAttackParam);
            }
        }

        List<GameObject> fences = Room.FindBuffTargets(this, GameObjectType.Fence, SkillRange);
        if (fences.Count != 0)
        {
            foreach (var fence in fences)
            {
                fence.Hp += HealParam;
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