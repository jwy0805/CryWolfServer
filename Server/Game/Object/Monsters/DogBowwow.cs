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

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Warrior;
    }

    public override void Update()
    {
        base.Update();
        FindOtherDogs();
    }

    protected override void UpdateMoving()
    {
        if (Room == null) return;
        
        // Targeting
        Target = Room.FindClosestTarget(this);
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatDestPos = DestPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatDestPos, flatCellPos);
        double deltaX = DestPos.X - CellPos.X;
        double deltaZ = DestPos.Z - CellPos.Z;
        Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
        
        if (distance <= TotalAttackRange)
        {
            State = State.Attack;
            SyncPosAndDir();
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
    
    public override void ApplyAttackEffect(GameObject target)
    {
        if (Room == null || AddBuffAction == null) return;
        
        HitCount++;
        if ((_smash && HitCount < 3) || (_smash == false && HitCount < 4))
            Room.Push(target.OnDamaged, this, TotalAttack, Damage.Normal, false);
    }

    protected override void SkillImpactEvents(long impactTime)
    {
        AttackTaskId = Scheduler.ScheduleCancellableEvent(impactTime, () =>
        {
            if (Room == null || AddBuffAction == null) return;
            if (Target == null || Target.Targetable == false || Hp <= 0) return;
            if (State == State.Faint) return;
            if (_smash)
            {
                Room.Push(Target.OnDamaged, this, TotalSkillDamage, Damage.True, false);
                if (_smashFaint)
                {
                    var randomInt = new Random().Next(100);
                    if (randomInt <= 15)
                    {
                        Room.Push(AddBuffAction, 
                            BuffId.Fainted, BuffParamType.None, Target, this, 0, 1000, false);
                    }
                }
            }
            else
            {
                Room.Push(Target.OnDamaged, this, TotalSkillDamage, Damage.True, false);
            }
            
            HitCount = 0;
        });
    }

    protected override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false || Target.Hp <= 0 || Target.Room == null)
        {
            State = State.Idle;
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(this, Target);
        Vector3 flatTargetPos = targetPos with { Y = 0 };
        Vector3 flatCellPos = CellPos with { Y = 0 };
        float distance = Vector3.Distance(flatTargetPos, flatCellPos);  

        if (distance > TotalAttackRange)
        {
            State = State.Idle;
            return;
        } 

        if (_smash) State = HitCount >= 2 ? State.Skill2 : GetRandomState(State.Attack, State.Attack2);
        else State = HitCount >= 3 ? State.Skill : GetRandomState(State.Attack, State.Attack2);
        SyncPosAndDir();
    }
}