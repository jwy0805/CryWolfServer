using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothMoon : MothLuna
{
    private readonly int _debuffRemoveParam = 40;
    protected readonly int HealParam = 20;
    protected readonly int OutputParam = 10;
    private bool _debuffRemoveSheep = false;
    private bool _healSheep = false;
    private bool _output = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothMoonRemoveDebuffSheep:
                    _debuffRemoveSheep = true;
                    break;
                case Skill.MothMoonHealSheep:
                    _healSheep = true;
                    break;
                case Skill.MothMoonRange:
                    AttackRange += 3;
                    break;
                case Skill.MothMoonOutput:
                    _output = true;
                    break;
                case Skill.MothMoonAttackSpeed:
                    AttackSpeed += 0.15f;
                    break;
            }
        }
    }

    protected override void UpdateIdle()
    {
        GameObject? target = Room?.FindNearestTarget(this, AttackType);
        if (target == null) return;
        Target ??= target;
        if (Target == null) return;

        StatInfo targetStat = Target.Stat;
        if (targetStat.Targetable)
        {
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(Target.CellPos - CellPos));
            double deltaX = Target.CellPos.X - CellPos.X;
            double deltaZ = Target.CellPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            if (distance <= AttackRange)
            {
                State = State.Attack;
                BroadcastMove();
            }
        }
    }
    
    protected override void UpdateAttack()
    {
        if (Target == null || Target.Stat.Targetable == false)
        {
            State = State.Idle;
            BroadcastMove();
        }
    }

    public override void RunSkill()
    {
        if (Room == null) return;
        List<GameObject> sheeps = Room.FindTargets(this, 
            new List<GameObjectType> { GameObjectType.Sheep }, AttackRange);
        Random random = new Random();
        
        if (sheeps.Any())
        {
            foreach (var s in sheeps)
            {
                if (_healSheep)
                {
                    s.Hp += HealParam;
                    Room.Broadcast(new S_ChangeHp { ObjectId = Id, Hp = Hp });
                }
                
                if (_debuffRemoveSheep)
                {
                    int r = random.Next(99);
                    if (r < _debuffRemoveParam) BuffManager.Instance.RemoveAllDebuff(this);
                }

                if (_output)
                {
                    Sheep sheep = (Sheep)s;
                    sheep.YieldIncrement = sheep.Resource * OutputParam / 100;
                }
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
                    State = State.Attack;
                    SetDirection();
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