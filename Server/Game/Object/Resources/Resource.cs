using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Util;

namespace Server.Game.Resources;

public class Resource : GameObject
{
    public int ResourceNum;
    public ResourceId ResourceId;

    private long _activeTime;
    public int Yield;
    private readonly float _dist = 8f;
    
    protected Resource()
    {
        ObjectType = GameObjectType.Resource;
    }

    public override void Init()
    {
        if (Room != null) _activeTime = Room.Stopwatch.ElapsedMilliseconds + 400;
    }

    public override void Update()
    {
        base.Update();
        if (Room!.Stopwatch.ElapsedMilliseconds < _activeTime) return;

        switch (State)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Moving:
                UpdateMoving();
                break;
        }
    }

    protected virtual void UpdateIdle()
    {
        double dist = Math.Sqrt(new Vector3().SqrMagnitude(Player.CellPos - CellPos));
        if ((float)dist < _dist)
        {
            State = State.Moving;
            BroadcastMove();
        }
    }

    protected virtual void UpdateMoving()
    {
        DestPos = Player.CellPos;
        DestVector destVector = new DestVector { X = DestPos.X, Y = DestPos.Y, Z = DestPos.Z };
        Room?.Broadcast(new S_SetDestResource
        {
            ObjectId = Id,
            Yield = Yield,
            Dest = destVector
        });
    }
}