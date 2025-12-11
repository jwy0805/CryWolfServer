using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class PrimeSheepWhite : Sheep
{
    private bool _tutorialYield;
    
    public override void Init()
    {
        base.Init();
        SheepBoundMargin = 3f;
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
        // Tutorial
        if (Room.GameMode == GameMode.Tutorial && !_tutorialYield && Room.Round == 0 && Room.RoundTime < 17)
        {
            var tutorialParam = (int)((Room.GameInfo.TotalSheepYield + YieldIncrement - YieldDecrement) * YieldInterval * 2);
            Room.YieldCoin(this, Math.Clamp(tutorialParam, 0, tutorialParam));
            _tutorialYield = true;
        }
        
        if (Room.Stopwatch.ElapsedMilliseconds <= Time + YieldTime || State == State.Die || Room.Round == 0) return;
        var param = (int)((Room.GameInfo.TotalSheepYield + YieldIncrement - YieldDecrement) * YieldInterval * 2);
        Time = Room.Stopwatch.ElapsedMilliseconds;
        Room.YieldCoin(this, Math.Clamp(param, 0, param));
    }
}