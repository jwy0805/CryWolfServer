using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game.Resources;

public class Resource : GameObject
{
    protected readonly float WaitTime = 1.0f;
    protected readonly Scheduler Scheduler = new();
    
    public ResourceId ResourceId { get; set; }
    public int Yield { get; set; }
    
    protected Resource()
    {
        ObjectType = GameObjectType.Resource;
    }

    public override void Init()
    {
        if (Room == null) return;
        DestPos = Player.CellPos;
        MoveSpeed = 8;
        CalculateYieldTime();
    }

    private void CalculateYieldTime()
    {
        var distance = Vector3.Distance(DestPos, CellPos);
        long yieldTime = (long)(distance / MoveSpeed * 1000);
        IncreaseResource(yieldTime);
    }
    
    protected virtual async void IncreaseResource(long time)
    {
        if (Room == null) return;
        await Scheduler.ScheduleEvent(time, () =>
        {
            Room.Push(() =>
            {
                if (Room == null) return;
                if (Player.Faction == Faction.Sheep)
                {
                    Room.GameInfo.SheepResource += Yield;
                }
                else
                {
                    Room.GameInfo.WolfResource += Yield;
                }
            });
            
            Room.Push(Room.LeaveGame, Id);
        });
    }
}