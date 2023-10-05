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
    protected readonly int OutputParam = 110;
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
                    _output = _debuffRemoveSheep;
                    break;
                case Skill.MothMoonAttackSpeed:
                    AttackSpeed += 0.15f;
                    break;
            }
        }
    }

    public override void RunSkill()
    {
        if (Room == null) return;
        List<GameObject> sheeps = Room.FindBuffTargets(this, GameObjectType.Sheep, 20);
        Random random = new Random();
        
        if (sheeps.Count != 0)
        {
            foreach (var sheep in sheeps)
            {
                if (_healSheep)
                {
                    sheep.Hp += HealParam;
                }
                
                if (_debuffRemoveSheep)
                {
                    int r = random.Next(99);
                    if (r < _debuffRemoveParam) BuffManager.Instance.RemoveAllBuff(this);
                }

                if (_output)
                {
                    sheep.Resource *= OutputParam / 100;
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