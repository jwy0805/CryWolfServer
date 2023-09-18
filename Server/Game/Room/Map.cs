using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class Map
{
    public bool ApplyMap(GameObject gameObject)
    {
        ApplyLeave(gameObject);
        if (gameObject.Room == null) return false;
        if (gameObject.Room.Map != this) return false;
       
        StatInfo stat = gameObject.Stat;
        Vector2Int v = Vector3To2(new Vector3(gameObject.PosInfo.PosX, gameObject.PosInfo.PosY, gameObject.PosInfo.PosZ));
        bool canGo = CanGo(gameObject, v, true, gameObject.Stat.SizeX);
        
        if (canGo == false) return false;
        
        PositionInfo posInfo = gameObject.PosInfo;
        int x = (int)(posInfo.PosX * 4 - MinX);
        int z = (int)(MaxZ - posInfo.PosZ * 4);
        int xSize = stat.SizeX;
        int zSize = stat.SizeZ;
        List<(int, int)> coordinate = new List<(int, int)>();
        
        if (xSize != zSize)
        {
            if (gameObject.PosInfo.Dir < 0) gameObject.PosInfo.Dir = 360 + gameObject.PosInfo.Dir;
            
            for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
            {
                for (int j = z - (zSize - 1); j <= z + (zSize - 1); j++)
                {
                    coordinate.Add(gameObject.PosInfo.Dir is (> 45 and < 135) or (> 225 and < 315) ? (i, j) : (j, i));
                }
            }
        }
        else
        {
            for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
            {
                for (int j = z - (xSize - 1); j <= z + (xSize - 1); j++)
                {
                    coordinate.Add((j, i));
                }
            }
        }
        
        //
        // coordinate.Add((z, x));
        //

        switch (stat.UnitType)
        {
            case 0: // 0 -> ground
                foreach (var tuple in coordinate) Objects[tuple.Item2, tuple.Item1] = gameObject;
                break;
            case 1: // 1 -> air
                foreach (var tuple in coordinate) _objectsAir[tuple.Item2, tuple.Item1] = gameObject;
                break;
            case 2: // 2 -> player
                foreach (var tuple in coordinate) _objectPlayer[tuple.Item2, tuple.Item1] = 1;
                break;
            default:
                break;
        }

        return true;
    }
        
    public bool ApplyLeave(GameObject gameObject)
    {
        if (gameObject.Room == null) return false;
        if (gameObject.Room.Map != this) return false;
    
        PositionInfo posInfo = gameObject.PosInfo;
        StatInfo stat = gameObject.Stat;
        if (posInfo.PosX < MinX || posInfo.PosX > MaxX) return false;
        if (posInfo.PosZ < MinZ || posInfo.PosZ > MaxZ) return false;

        int x = (int)(posInfo.PosX * 4 - MinX);
        int z = (int)(MaxZ - posInfo.PosZ * 4);
        int xSize = stat.SizeX;
        int zSize = stat.SizeZ;
        List<(int, int)> coordinate = new List<(int, int)>();

        if (xSize != zSize)
        {
            if (gameObject.PosInfo.Dir < 0) gameObject.PosInfo.Dir = 360 + gameObject.PosInfo.Dir;
        
            for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
            {
                for (int j = z - (zSize - 1); j <= z - (zSize - 1); j++)
                {
                    coordinate.Add(gameObject.PosInfo.Dir is (> 45 and < 135) or (> 225 and < 315) ? (i, j) : (j, i));
                }
            }
        }
        else
        {
            for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
            {
                for (int j = z - (xSize - 1); j <= z - (xSize - 1); j++)
                {
                    coordinate.Add((j, i));
                }
            }
        }

        switch (stat.UnitType)
        {
            case 0: // 0 -> ground
                foreach (var tuple in coordinate) Objects[tuple.Item2, tuple.Item1] = null;
                break;
            case 1: // 1 -> air
                foreach (var tuple in coordinate) _objectsAir[tuple.Item2, tuple.Item1] = null;
                break;
            case 2: // 2 -> player
                foreach (var tuple in coordinate) _objectPlayer[tuple.Item2, tuple.Item1] = 0;
                break;
            default:
                break;
        }

        return true;
    }
     
    public GameObject? Find(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return null;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return null;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)((MaxZ - cellPos.Z) * 4);
        return Objects[z, x];
    }
    
    public bool CanGo(GameObject go, Vector2Int cellPos, bool checkObjects = true, int size = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = cellPos.X - MinX;
        int z = MaxZ - cellPos.Z;
        
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z + (size - 1); j++)
            {
                if (!_collision[j, i] && Objects[j, i] == null) continue;
                if (Objects[j, i]?.Id != go.Id) cnt++;
            }
        }
        
        return cnt == 0 || !checkObjects;
    } 
    
    public bool CanGoAir(GameObject go, Vector2Int cellPos, bool checkObjects = true, int size = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = cellPos.X - MinX;
        int z = MaxZ - cellPos.Z;
        
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z + (size - 1); j++)
            {
                if (!_collision[j, i] && _objectsAir[j, i] == null) continue;
                if (_objectsAir[j, i]?.Id != go.Id) cnt++;
            }
        }
        
        return cnt == 0 || !checkObjects;
    }
    
    public (List<Vector3>, List<Vector3>, List<double>) Move(GameObject gameObject, Vector3 s, Vector3 d)
    {
        Vector2Int startCell = Vector3To2(s);
        Vector2Int destCell = Vector3To2(d);
        int startRegionId = GetRegionByVector(Cell2Pos(startCell));
        int destRegionId = GetRegionByVector(Cell2Pos(destCell));
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        List<Vector2Int> center = regionPath.Select(t => Pos2Cell(_regionGraph[t].CenterPos)).ToList();
        List<Vector3> path = new List<Vector3>();
        List<double> arctan = new List<double>();
        Vector2Int start = startCell;

        if (regionPath.Count == 0)
        {
            path = FindPath(gameObject, startCell, destCell);
        }
        else
        {
            for (int i = 0; i < center.Count; i++)
            {
                // Console.WriteLine($"{center[i].X}, {center[i].Z}");
                List<Vector3> aStar = FindPath(gameObject, start, center[i]);
                path.AddRange(aStar);
                start = Vector3To2(path.Last());
            }

            List<Vector3> lastPath = FindPath(gameObject, center.Last(), destCell);
            path.AddRange(lastPath);
        }

        List<Vector3> uniquePath = path.Distinct().ToList();

        arctan.Add(Math.Round(Math.Atan2(uniquePath[0].X - startCell.X, uniquePath[0].Z - startCell.Z))); // i = 0
        for (int i = 1; i < uniquePath.Count; i++)
        {
            double atan2 = 
                Math.Round(Math.Atan2(uniquePath[i].X - uniquePath[i - 1].X, uniquePath[i].Z - uniquePath[i - 1].Z) * (180 / Math.PI), 2);
            arctan.Add(atan2);
        }

        List<Vector3> destList = new List<Vector3>();
        List<double> atanList = new List<double>();
        for (int i = 1; i < arctan.Count - 1; i++)
        {
            if (Math.Abs(arctan[i] - arctan[i + 1]) > 0.001f) // float 비교 -> arctan[i] != arctan[i + 1], Tolerance = 0.001f
            {
                destList.Add(uniquePath[i]);
            }
        }
        
        for (int i = 0; i < arctan.Count - 1; i++)
        {
            if (Math.Abs(arctan[i] - arctan[i + 1]) > 0.001f) // float 비교 -> arctan[i] != arctan[i + 1], Tolerance = 0.001f
            {
                atanList.Add(arctan[i + 1]);
            }
        }

        destList.Add(uniquePath[^1]);

        return (path, destList, atanList);
    }

    public void LoadMap(int mapId = 1, string pathPrefix = "/Users/jwy/Documents/dev/CryWolf/Common/MapData")
    {
        MinX = -400;
        MaxX = 400;
        MinZ = -400;
        MaxZ = 400;

        int xCount = MaxX - MinX + 1;
        int zCount = MaxZ - MinZ + 1;
        _collision = new bool[zCount, xCount];
        Objects = new GameObject[zCount, xCount];
        _objectsAir = new GameObject[zCount, xCount];
        
        // Collision 관련 파일
        string mapName = "Map_" + mapId.ToString("000");
        string txt = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
        StringReader reader = new StringReader(txt);
        for (int z = 0; z < zCount; z++)
        {
            string? line = reader.ReadLine();
            for (int x = 0; x < xCount; x++)
            {
                if (line != null)
                {
                    _collision[z, x] = line[x] == '2' || line[x] == '4';
                    // _collisionAir[z, x] = line[x] == '4';
                }
            }
        }
    }

    public Vector3 FindSpawnPos(GameObject gameObject, SpawnWay? way = null)
    {
        GameObjectType type = gameObject.ObjectType;
        Vector3 cell = new Vector3();
        if (type == GameObjectType.Monster)
        {
            switch (way)
            {
                case SpawnWay.West:
                    cell = GameData.SpawnerPos[0];
                    break;
                case SpawnWay.North:
                    cell = GameData.SpawnerPos[1];
                    break;
                case SpawnWay.East:
                    cell = GameData.SpawnerPos[2];
                    break;
                default:
                    cell = GameData.SpawnerPos[1];
                    break;
            }
        }
        else if (type == GameObjectType.Sheep)
        {
            cell = new Vector3(0, 6, 0);
        }

        Pos pos = FindNearestEmptySpace(Cell2Pos(Vector3To2(cell)), gameObject, gameObject.Stat.SizeX, gameObject.Stat.SizeX);
        Vector3 result = Vector2To3(Pos2Cell(pos));

        return result;
    }
}