using Google.Protobuf.Protocol;

namespace Server.Game;

public class PrimeSheepBlack : Sheep
{
    public override void Init()
    {
        base.Init();
        SheepBoundMargin = 3f;
        Stat.Hp += 40;
        Stat.MaxHp += 40;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        Yield();
        
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
            case State.KnockBack:
                UpdateKnockBack();
                break;
            case State.Faint:
                break;
            case State.Standby:
                break;
        }   
    }

    protected override void Yield()
    {
        if (Room == null) return;
        if (Room.Stopwatch.ElapsedMilliseconds <= Time + YieldTime || State == State.Die || Room.Round == 0) return;
        var param = (int)((Room.GameInfo.TotalSheepYield + YieldIncrement - YieldDecrement) * YieldInterval * 2);
        Time = Room.Stopwatch.ElapsedMilliseconds;
        Room.YieldCoin(this, Math.Clamp(param, 0, param));
    }
}