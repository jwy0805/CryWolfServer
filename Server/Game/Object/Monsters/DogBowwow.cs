using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class DogBowwow : DogBark
{
    private Random _rnd = new();
    private bool _smash = false;
    private bool _smashFaint = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.DogBowwowSmash:
                    _smash = true;
                    break;
                case Skill.DogBowwowSmashFaint:
                    _smash = false;
                    break;
            }
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastPos();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastPos();
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));

        if (distance > TotalAttackRange)
        {
            DestPos = targetPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
            return;
        }

        if (_4hitCount == 3)
        {
            State = _smash ? State.Skill2 : State.Skill;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            State = _rnd.Next(2) == 0 ? State.Attack : State.Attack2;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
    }

    public override void SetNormalAttackEffect(GameObject target)
    {
        _4hitCount++;
    }

    public override void SetAdditionalAttackEffect(GameObject target)
    {
        _4hitCount = 0;
        
        if (_smash)
        {
            target.OnDamaged(this, TotalSkillDamage, Damage.True);
            if (_smashFaint)
            {
                BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1000);
            }
        }
        else
        {
            target.OnDamaged(this, 50, Damage.True);
        }
    }
}