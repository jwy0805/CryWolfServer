using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class SnakeNaga : Snake
{
    private bool _drain = false;
    private bool _meteor = true;
    private readonly float _drainParam = 0.2f;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.SnakeNagaBigFire:
                    Room?.Broadcast(new S_SkillUpdate
                    {
                        ObjectEnumId = (int)UnitId,
                        ObjectType = GameObjectType.Monster,
                        SkillType = SkillType.SkillProjectile
                    });
                    break;
                case Skill.SnakeNagaDrain:
                    _drain = true;
                    break;
                case Skill.SnakeNagaCritical:
                    CriticalChance += 30;
                    break;
                case Skill.SnakeNagaSuperAccuracy:
                    Accuracy += 70;
                    break;
                case Skill.SnakeNagaMeteor:
                    _meteor = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);

        if (MaxMp != 1 && Mp >= MaxMp && _meteor)
        {
            State = State.Skill;
            BroadcastPos();
            Mp = 0;
        }
        else
        {
            switch (State)
            {
                case State.Die:
                    UpdateDie();
                    break;
                case State.Moving:
                    UpdateMoving();
                    break;
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Skill:
                    UpdateSkill();
                    break;
                case State.KnockBack:
                    UpdateKnockBack();
                    break;
                case State.Faint:
                    break;
                case State.Standby:
                    break;
            }
        }
    }
    
    public override void ApplyNormalAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);
        if (_drain) Hp += (int)((TotalAttack - target.TotalDefence) * _drainParam);
        BuffManager.Instance.AddBuff(BuffId.Burn, target, this, 5f);
    }
}