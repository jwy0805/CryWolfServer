using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MosquitoBug : Monster
{
    private bool _faint = false;

    protected int FaintParam = 30;
    protected List<GameObjectType> _typeList = new() { GameObjectType.Sheep };
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MosquitoBugEvasion:
                    Evasion += 10;
                    break;
                case Skill.MosquitoBugRange:
                    AttackRange += 1;
                    break;
                case Skill.MosquitoBugSpeed:
                    MoveSpeed += 1;
                    break;
                case Skill.MosquitoBugSheepFaint:
                    _faint = true;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        UnitRole = Role.Mage;
    }

    protected override void UpdateIdle()
    {
        Target = Room.FindClosestTarget(this, _typeList, Stat.AttackType);
        if (Target == null || Target.Targetable == false || Target.Room != Room) return;
        State = State.Moving;
    }

    protected override void UpdateMoving()
    { // Targeting
        Target = Room.FindClosestTarget(this, _typeList, Stat.AttackType); 
        if (Target == null || Target.Targetable == false || Target.Room != Room)
        {   // Target이 없거나 타겟팅이 불가능한 경우
            State = State.Idle;
            return;
        }
        // Target과 GameObject의 위치가 Range보다 짧으면 ATTACK
        DestPos = Room.Map.GetClosestPoint(CellPos, Target);
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

    public override void ApplyAttackEffect(GameObject target)
    {
        target.OnDamaged(this, TotalAttack, Damage.Normal);       
        if (target is Sheep _ && _faint && new Random().Next(100) < FaintParam)
        {
            BuffManager.Instance.AddBuff(BuffId.Fainted, BuffParamType.None, target, this, 0, 1000);
        }
    }
}