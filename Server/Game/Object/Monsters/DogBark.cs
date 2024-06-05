using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class DogBark : DogPup
{
    private bool _attackSpeedUp = false;
    private bool _4Hit = false;
    private HashSet<int> _currentSet = new();
    protected short HitCount = 0;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.DogBarkAdjacentAttackSpeed:
                    _attackSpeedUp = true;
                    break;
                case Skill.DogBarkFireResist:
                    FireResist += 15;
                    break;
                case Skill.DogBarkFourthAttack:
                    _4Hit = true;
                    break;
            }
        }
    }
    
    public override void Update()
    {
        base.Update();
        if (_attackSpeedUp) FindOtherDogs();
    }

    protected override void UpdateSkill()
    {
        UpdateAttack();
    }
    
    protected void FindOtherDogs()
    {
        var unitIds = new List<UnitId> { UnitId.DogPup, UnitId.DogBark, UnitId.DogBowwow };
        var otherDogs = Room?
            .FindTargetsBySpecies(this, GameObjectType.Monster, unitIds, SkillRange);
        if (otherDogs == null) return;

        var newSet = new HashSet<int>(otherDogs
            .Where(dog => dog.Id != Id)
            .Select(dog => dog.Id));
        // 추가된 유닛에 버프 적용
        foreach (var dogId in newSet.Where(dogId => _currentSet.Contains(dogId) == false))
        {
            if (Room?.FindGameObjectById(dogId) is Creature creature) creature.AttackSpeedParam += 0.05f;
        }
        // 제거된 유닛에서 버프 제거
        foreach (var dogId in _currentSet.Where(dogId => newSet.Contains(dogId) == false))
        {
            if (Room?.FindGameObjectById(dogId) is Creature creature) creature.AttackSpeedParam -= 0.05f;
        }
        // 현재 상태를 새로운 상태로 업데이트
        _currentSet = newSet;
    }
   
    public override void ApplyNormalAttackEffect(GameObject target)
    {
        if (_4Hit)
        {
            HitCount++;
            if (HitCount == 4)
            {
                HitCount = 0;
                target.OnDamaged(this, TotalSkillDamage, Damage.True);
                return;
            }
        }
        
        target.OnDamaged(this, TotalAttack, Damage.Normal);
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
            return;
        }
        
        Vector3 targetPos = Room.Map.GetClosestPoint(CellPos, Target);
        float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - CellPos));

        if (distance > TotalAttackRange)
        {
            State = State.Moving;
            return;
        }

        if (_4Hit && HitCount == 3) State = State.Skill;
        else State = State.Attack;
        SetDirection();
    }
}