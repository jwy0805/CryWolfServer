using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Sheep : GameObject
{
    public int SheepNo = 1;

    public Sheep()
    {
        ObjectType = GameObjectType.Sheep;
    }

    public void Init()
    {
        DataManager.ObjectDict.TryGetValue(SheepNo ,out var objectData);
        Stat.MergeFrom(objectData!.stat);
        Stat.Hp = objectData.stat.MaxHp;

        State = State.Idle;
    }

    protected override void UpdateIdle()
    {
        DestPos = GetDestInFence();
        (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
        BroadcastDest();
        State = State.Moving;
    }

    protected override void UpdateMoving()
    {
        
    }

    private Vector3 GetDestInFence()
    {
        int level = Room!.StorageLevel;
        List<Vector3> sheepBound = GameData.SheepBounds[level];
        float minX = sheepBound.Select(v => v.X).ToList().Min();
        float maxX = sheepBound.Select(v => v.X).ToList().Max();
        float minZ = sheepBound.Select(v => v.Z).ToList().Min();
        float maxZ = sheepBound.Select(v => v.Z).ToList().Max();

        do
        {
            Random random = new();
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = new Vector3(x, 6.0f, z);
            bool canGo = Room!.Map.CanGoGround(dest);
            if (canGo) break;
        } while (true);
        
        return new Vector3();
    }
}