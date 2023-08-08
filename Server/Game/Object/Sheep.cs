using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public class Sheep : GameObject
{
    public int SheepNo = 1;
    private Stopwatch _stopwatch;
    private long _lastSetDest = 0;
    private int _tmpcnt;

    public Sheep()
    {
        ObjectType = GameObjectType.Sheep;
    }

    public void Init()
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        DataManager.ObjectDict.TryGetValue(SheepNo ,out var objectData);
        Stat.MergeFrom(objectData!.stat);
        Stat.Hp = objectData.stat.MaxHp;

        State = State.Idle;
    }

    protected override void UpdateIdle()
    {
        if (_tmpcnt > 0)
        {
            Console.WriteLine(_stopwatch.ElapsedMilliseconds);
        }
        if (_stopwatch.ElapsedMilliseconds > _lastSetDest + new Random().Next(1000, 2500))
        {
            _lastSetDest = _stopwatch.ElapsedMilliseconds;
            DestPos = GetRandomDestInFence();
            (Path, Atan) = Room!.Map.Move(this, CellPos, DestPos);
            BroadcastDest();
            Console.WriteLine(_stopwatch.ElapsedMilliseconds);
            State = State.Moving;
        }

    }

    protected override void UpdateMoving()
    {
        if (Room != null)
        {
            // 이동
            float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(DestPos - CellPos)); // 거리의 제곱
            double deltaX = DestPos.X - CellPos.X;
            double deltaZ = DestPos.Z - CellPos.Z;
            Dir = (float)Math.Round(Math.Atan2(deltaX, deltaZ) * (180 / Math.PI), 2);
            Console.WriteLine($"Dist: {distance} / {DestPos.X}, {DestPos.Z}");
            
            if (distance < 0.1)
            {
                _tmpcnt++;
                Console.WriteLine(_tmpcnt);
                State = State.Idle;
                BroadcastMove();
                return;
            }
            
            BroadcastMove();
        }
    }

    private Vector3 GetRandomDestInFence()
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
            Map map = Room!.Map;
            float x = Math.Clamp((float)random.NextDouble() * (maxX - minX) + minX, minX, maxX);
            float z = Math.Clamp((float)random.NextDouble() * (maxZ - minZ) + minZ, minZ, maxZ);
            Vector3 dest = Util.Util.NearestCell(new Vector3(x, 6.0f, z));
            Console.WriteLine($"{x}, {z} / {dest.X}, {dest.Z}");
            bool canGo = map.CanGoGround(dest);
            if (canGo) return dest;
        } while (true);
    }

    private async Task Wait()
    {
        Random random = new Random();
        int millisecondsToDelay = random.Next(1000, 2501);
        await Task.Delay(millisecondsToDelay);
    }
}