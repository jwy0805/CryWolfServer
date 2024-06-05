using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class DogBowwow : DogBark
{
    private bool _smash;
    private bool _smashFaint;
    
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
                    SkillDamage += 20;
                    break;
                case Skill.DogBowwowSmashFaint:
                    _smashFaint = true;
                    break;
            }
        }
    }

    public override void Update()
    {
        base.Update();
        FindOtherDogs();
    }

    protected override void UpdateMoving()
    {   // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        if (distance <= TotalAttackRange)
        {
            if (_smash) State = HitCount == 2 ? State.Skill2 : GetRandomState(State.Attack, State.Attack2);
            else State = HitCount == 3 ? State.Skill : GetRandomState(State.Attack, State.Attack2);
            return;
        }
        // Target이 있으면 이동
        (Path, Atan) = Room.Map.Move(this);
        BroadcastPath();
    }
    
    protected override void UpdateAttack2()
    {
        UpdateAttack();
    }
    
    protected override void UpdateSkill()
    {
        UpdateAttack();
    }

    protected override void UpdateSkill2()
    {
        UpdateAttack();
    }
    
    public override void ApplyNormalAttackEffect(GameObject target)
    {
        HitCount++;
        if ((_smash && HitCount == 3) || (_smash == false && HitCount == 4))
        {
            HitCount = 0;
            target.OnDamaged(this, TotalSkillDamage, Damage.True);
            if (_smash == false || _smashFaint == false) return;
            var randomInt = new Random().Next(100);
            if (randomInt > 15) return;
            BuffManager.Instance.AddBuff(BuffId.Fainted, target, this, 0, 1000);
        }
        else
        {
            target.OnDamaged(this, TotalAttack, Damage.Normal);
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
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
            State = State.Moving;
            return;
        }

        if (_smash) State = HitCount == 2 ? State.Skill2 : GetRandomState(State.Attack, State.Attack2);
        else State = HitCount == 3 ? State.Skill : GetRandomState(State.Attack, State.Attack2);
        SetDirection();
    }
}