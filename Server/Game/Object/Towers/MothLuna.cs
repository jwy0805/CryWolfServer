using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game;

public class MothLuna : Tower
{
    protected Vector3 StartCell;
    protected long LastSetDest = 0;

    private bool _faint = false;
    
    protected override Skill NewSkill
    {
        get => Skill;
        set
        {
            Skill = value;
            switch (Skill)
            {
                case Skill.MothLunaAttack:
                    Attack += 3;
                    break;
                case Skill.MothLunaAccuracy:
                    Accuracy += 5;
                    break;
                case Skill.MothLunaFaint:
                    _faint = true;
                    break;
                case Skill.MothLunaSpeed:
                    MoveSpeed += 2;
                    break;
            }
        }
    }

    public override void Init()
    {
        base.Init();
        StartCell = CellPos;
    }
    
    protected override void UpdateIdle()
    {
        
        if (Room?.Stopwatch.ElapsedMilliseconds > LastSetDest + new Random().Next(500, 1500))
        {
            LastSetDest = Room.Stopwatch.ElapsedMilliseconds;
            DestPos = GetRandomDestInFence();
            (Path, Dest, Atan) = Room!.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            State = State.Moving;
        }
    }
    
    protected override void UpdateMoving()
    {
        if (Room == null) return;
        
        GameObject? target = Room.FindMosquitoInFence();
        if (Target == null && target != null)
        {
            Target = target;
            DestPos = Target.CellPos;
            (Path, Dest, Atan) = Room.Map.Move(this, CellPos, DestPos);
        }
    }
}