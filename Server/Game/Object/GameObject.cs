using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class GameObject
{
    public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
    public int Id
    {
        get => Info.ObjectId;
        set => Info.ObjectId = value;
    }

    public GameRoom? Room { get; set; }
    public ObjectInfo Info { get; set; } = new();
    public PositionInfo PosInfo { get;  set; } = new();

    public State State
    {
        get => PosInfo.State;
        set => PosInfo.State = value;
    }

    public GameObject()
    {
        Info.PosInfo = PosInfo;
    }
    
    public virtual void Update() { }

    public Vector3 CellPos
    {
        get => new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
        set
        {
            PosInfo.PosX = value.X;
            PosInfo.PosY = value.Y;
            PosInfo.PosZ = value.Z;
        }
    }
}