using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class DogBark : DogPup
{
    private bool _attackSpeedUp = false;
    private bool _4hit = false;
    private HashSet<int> _preSet = new();
    private HashSet<int> _currentSet = new();
    protected short _4hitCount = 0;
    
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
                    _4hit = true;
                    break;
            }
        }
    }

    public override void Update()
    {
        base.Update();
        FindOtherDogs();
    }

    private void FindOtherDogs()
    {
        var unitIds = new List<UnitId> { UnitId.DogPup, UnitId.DogBark, UnitId.DogBowwow };
        var otherDogs = Room?
            .FindTargetsBySpecies(this, GameObjectType.Monster, unitIds, SkillRange);
        if (otherDogs == null || otherDogs.Count == 0) return;

        foreach (var dog in _currentSet) _preSet.Add(dog);
        foreach (var dog in otherDogs.Where(dog => dog.Id != Id)) _currentSet.Add(dog.Id);   
        
        var diffSetC = new HashSet<int>(_currentSet);
        diffSetC.ExceptWith(_preSet);
        foreach (var dogId in diffSetC)
        {
            if (Room?.FindGameObjectById(dogId) is not Creature creature) continue;
            creature.AttackParam += 3;
        }
        
        var diffSetP = new HashSet<int>(_preSet);
        diffSetP.ExceptWith(_currentSet);
        foreach (var dogId in diffSetP)
        {
            if (Room?.FindGameObjectById(dogId) is not Creature creature) continue;
            creature.AttackParam -= 3;
        }
    }
    
    public override void SetNextState()
    {
        if (Room == null) return;
        if (Target == null || Target.Targetable == false)
        {
            State = State.Idle;
            BroadcastMove();
            return;
        }

        if (Target.Hp <= 0)
        {
            Target = null;
            State = State.Idle;
            BroadcastMove();
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
            State = State.Skill;
            Room.Broadcast(new S_State { ObjectId = Id, State = State });
        }
        else
        {
            State = State.Attack;
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
        target.OnDamaged(this, TotalSkillDamage, Damage.True);
    }
}