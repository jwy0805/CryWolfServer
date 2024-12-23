using Google.Protobuf.Protocol;

namespace Server.Game;

public class PrimeSheepWhite : Sheep
{
    public override void Init()
    {
        base.Init();
        SheepBoundMargin = 3f;
    }

    public override void Update()
    {
        if (Room == null) return;
        Job = Room.PushAfter(CallCycle, Update);
        
        if (Room.Stopwatch.ElapsedMilliseconds > Time + YieldTime && State != State.Die)
        {
            var param = Room.GameInfo.TotalSheepYield * 2 + YieldIncrement - YieldDecrement;
            Time = Room.Stopwatch.ElapsedMilliseconds;
            Room.YieldCoin(this, Math.Clamp(param, 0, param));
        }
        
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
}