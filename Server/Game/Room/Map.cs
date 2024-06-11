using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class Map
{
    private readonly short _cellCnt = 4;
    public bool ApplyMap(GameObject gameObject, Vector3 pos = new(), bool checkObjects = true)
    {
        ApplyLeave(gameObject);
        if (gameObject.Room == null) return false;
        if (gameObject.Room.Map != this) return false;
       
        StatInfo stat = gameObject.Stat;
        Vector2Int v = Vector3To2(new Vector3(gameObject.PosInfo.PosX, gameObject.PosInfo.PosY, gameObject.PosInfo.PosZ));
        
        if (gameObject.ObjectType != GameObjectType.Fence)
        {
            bool canGo = CanGo(gameObject, v, checkObjects, gameObject.Stat.SizeX);
            if (canGo == false)
            {
                gameObject.BroadcastPos();
                return false;
            }
        }
        
        if (pos != Vector3.Zero)
        {
            gameObject.PosInfo.PosX = pos.X;
            gameObject.PosInfo.PosY = pos.Y;
            gameObject.PosInfo.PosZ = pos.Z;
        }
        
        List<(int, int)> coordinates = CalculateCoordinates(gameObject.PosInfo, stat);

        switch (gameObject.UnitType)
        {
            case 0: // 0 -> ground
                UpdateObjects(coordinates, _objects, gameObject);
                break;
            case 1: // 1 -> air
                UpdateObjects(coordinates, _objectsAir, gameObject);
                break;
            case 2: // 2 -> player
                foreach (var tuple in coordinates) _objectPlayer[tuple.Item1, tuple.Item2] = 1;
                break;
        }

        return true;
    }
        
    public bool ApplyLeave(GameObject gameObject)
    {
        if (gameObject.Room == null || gameObject.Room.Map != this) return false;
        PositionInfo posInfo = gameObject.PosInfo;
        StatInfo stat = gameObject.Stat;

        if (posInfo.PosX < MinX || posInfo.PosX > MaxX || posInfo.PosZ < MinZ || posInfo.PosZ > MaxZ) return false;
        List<(int, int)> coordinates = CalculateCoordinates(posInfo, stat);

        switch (stat.UnitType)
        {
            case 0: // 0 -> ground
                ClearObjects(coordinates, _objects);
                break;
            case 1: // 1 -> air
                ClearObjects(coordinates, _objectsAir);
                break;
            case 2: // 2 -> player
                foreach (var tuple in coordinates) _objectPlayer[tuple.Item1, tuple.Item2] = 0;
                break;
        }
        
        return true;
    }

    private List<(int, int)> CalculateCoordinates(PositionInfo posInfo, StatInfo stat)
    {
        int x = (int)(posInfo.PosX * 4 - MinX);
        int z = (int)(MaxZ - posInfo.PosZ * 4);
        int xSize = stat.SizeX;
        int zSize = stat.SizeZ;
        List<(int, int)> coordinates = new List<(int, int)>();
        
        if (xSize != zSize)
        {
            if (posInfo.Dir < 0) posInfo.Dir = 360 + posInfo.Dir;
            bool isVertical = posInfo.Dir is > 45 and < 135 or > 225 and < 315;
            if (isVertical)
            {
                for (int i = z - (xSize - 1); i <= z + (xSize - 1); i++)
                {
                    for (int j = x - (zSize - 1); j <= x + (zSize - 1); j++)
                    {
                        coordinates.Add((i, j));
                    }
                }
            }
            else
            {
                for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
                {
                    for (int j = z - (zSize - 1); j <= z + (zSize - 1); j++)
                    {
                        coordinates.Add((j, i));
                    }
                }
            }
        }
        else
        {
            for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
            {
                for (int j = z - (xSize - 1); j <= z + (xSize - 1); j++)
                {
                    coordinates.Add((j, i));
                }
            }
        }

        return coordinates;
    }

    private void UpdateObjects(List<(int, int)> coordinates, GameObject?[,] grid, GameObject go)
    {
        foreach (var (i, j) in coordinates)
        {
            grid[i, j] = go;
        }
    }

    private void ClearObjects(List<(int, int)> coordinates, GameObject?[,] grid)
    {
        foreach (var (i, j) in coordinates)
        {
            grid[i, j] = null;
        }
    }
    
    public GameObject? Find(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return null;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return null;

        int x = (int)((cellPos.X - MinX) * _cellCnt);
        int z = (int)((MaxZ - cellPos.Z) * _cellCnt);
        return _objects[z, x];
    }

    public bool CanSpawn(Vector2Int cellPos, int size)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        Pos pos = Cell2Pos(cellPos);
        int x = pos.X;
        int z = pos.Z;
        
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z + (size - 1); j++)
            {
                if (_objects[j, i] != null || _collision[j, i]) cnt++;
            }
        }

        return cnt == 0;
    }
    
    public bool CanGo(GameObject go, Vector2Int cellPos, bool checkObjects = true, int size = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        GameObject?[,] objects = go.UnitType == 0 ? _objects : _objectsAir;
        Pos pos = Cell2Pos(cellPos);
        int x = pos.X;
        int z = pos.Z;
        
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z + (size - 1); j++)
            {
                if (!_collision[j, i] && objects[j, i] == null) continue;
                if (objects[j, i]?.Id != go.Id && objects[j, i]?.Id != go.Target?.Id) cnt++;
            }
        }
        
        return cnt == 0 || !checkObjects;
    } 

    public (List<Vector3>, List<double>) Move(GameObject go, bool checkObjects = true)
    {
        Vector2Int startCell = Vector3To2(go.CellPos);
        Vector2Int destCell = Vector3To2(go.DestPos);
        if (CanGo(go, destCell, checkObjects) == false)
        {
            Vector2Int newDestCell = FindNearestEmptySpace(destCell, go);
            destCell = newDestCell;
        }
        
        int startRegionId = GetRegionByVector(startCell);
        int destRegionId = GetRegionByVector(destCell);
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        // Path 추출
        List<Vector2Int> center = new List<Vector2Int>();
        foreach (var region in regionPath) center.Add(GetCenter(region, go, startCell, destCell));
        List<double> arctan = new List<double>();
        Vector2Int destCellVector = regionPath.Count <= 1 ? destCell : center[1];
        List<Vector3> path = FindPath(go, startCell, destCellVector, checkObjects).Distinct().ToList();
        // if (path.Count == 0) return (new List<Vector3>(), new List<double>());
        // Dir(유닛이 어느 방향을 쳐다보는지) 추출
        for (int i = 0; i < path.Count - 1; i++)
        {
            double xDiff = path[i + 1].X - path[i].X;
            double zDiff = path[i + 1].Z - path[i].Z;
            double atan2 = Math.Round(Math.Atan2(xDiff, zDiff) * (180 / Math.PI), 2);
            arctan.Add(atan2);
        }
        if (arctan.Count > 0) arctan.Add(arctan[^1]);

        int moveTick = (int)(go.TotalMoveSpeed * _cellCnt * go.CallCycle / 1000 * 100 + go.DistRemainder);
        int index = 0;
        while (moveTick >= 100 && index < path.Count - 1)
        {
            double xDiff = path[index + 1].X - path[index].X;
            double zDiff = path[index + 1].Z - path[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;

            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }

        go.DistRemainder = moveTick;
        index = Math.Min(index, path.Count - 1);
        ApplyMap(go, path[index]);

        int indexRes = Math.Min(index * 8, path.Count);
        List<Vector3> pathRes = path.GetRange(0, indexRes);
        List<double> arcRes = arctan.GetRange(0, indexRes);
        return (pathRes, arcRes);
    }

    public List<Vector3> MoveIgnoreCollision(GameObject go)
    {
        Vector2Int startCell = Vector3To2(go.CellPos);
        Vector2Int destCell = Vector3To2(go.DestPos);
        if (CanGo(go, destCell, false) == false)
        {
            Vector2Int newDestCell = FindNearestEmptySpace(destCell, go);
            destCell = newDestCell;
        }
        
        int startRegionId = GetRegionByVector(startCell);
        int destRegionId = GetRegionByVector(destCell);
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        // Path 추출
        List<Vector2Int> center = new List<Vector2Int>();
        foreach (var region in regionPath) center.Add(GetCenter(region, go, startCell, destCell));
        Vector2Int destCellVector = regionPath.Count <= 1 ? destCell : center[1];
        List<Vector3> path = FindPath(go, startCell, destCellVector, false).Distinct().ToList();
        
        int moveTick = (int)(go.TotalMoveSpeed * _cellCnt * go.CallCycle / 1000 * 100 + go.DistRemainder);
        int index = 0;
        while (moveTick >= 100 && index < path.Count - 1)
        {
            double xDiff = path[index + 1].X - path[index].X;
            double zDiff = path[index + 1].Z - path[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;

            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }

        go.DistRemainder = moveTick;
        index = Math.Min(index, path.Count - 1);
        ApplyMap(go, path[index]);

        int indexRes = Math.Min(index * 8, path.Count);
        List<Vector3> pathRes = path.GetRange(0, indexRes);
        return pathRes;
    }

    public List<Vector3> ProjectileMove(Projectile p)
    {   
        List<Vector3> path = GetProjectilePath(p);
        int moveTick = (int)((p.MoveSpeed * _cellCnt * p.CallCycle / 1000 + p.DistRemainder) * 100);
        int index = 0;
        while (moveTick >= 1 && index < path.Count - 1)
        {
            double xDiff = path[index + 1].X - path[index].X;
            double zDiff = path[index + 1].Z - path[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;

            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }

        index = Math.Min(index, path.Count - 1);
        p.CellPos = path[index];
        int indexRes = Math.Min(index * 8, path.Count);
        return path.GetRange(0, indexRes);
    }

    public List<Vector3> GetProjectilePath(Projectile p)
    {   // Projectile은 맵의 collision, Objects와 충돌하지 않음
        Vector2Int startCell = Vector3To2(p.CellPos);
        Vector2Int destCell = Vector3To2(p.DestPos);
        List<Vector3> path = FindPath(p, startCell, destCell).Distinct().ToList();
        if (path.Count == 0) Console.WriteLine($"Cell: {p.CellPos}, Dest: {p.DestPos}");
        return path;
    }
    
    public (List<Vector3>, List<double>) KnockBack(GameObject go, double d, bool checkObjects = false)
    {   // KnockBack은 이동과 다르게 충돌체크 없이 순수 이동경로를 구한 후 이동 중 충돌하면 IDLE로 상태 변화
        Vector2Int startCell = Vector3To2(go.CellPos);
        Vector2Int destCell = Vector3To2(go.DestPos);
        List<Vector3> path = FindPath(go, startCell, destCell, checkObjects).Distinct().ToList();
        int moveTick = (int)((go.MoveSpeed * _cellCnt * go.CallCycle / 1000 + go.DistRemainder) * 100);
        int index = 0;
        while (moveTick >= 1 && index < path.Count)
        {
            double xDiff = path[index + 1].X - path[index].X;
            double zDiff = path[index + 1].Z - path[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;

            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }

        index = Math.Min(index, path.Count - 1);
        if (CanGo(go, destCell))
        {
            ApplyMap(go, path[index]);
            int indexRes = Math.Min(index * 8, path.Count - 1);
            List<Vector3> pathRes = path.GetRange(0, indexRes);
            List<double> dirRes = Enumerable.Repeat(d, pathRes.Count).ToList();
            return (pathRes, dirRes);
        }

        go.State = State.Idle;
        go.BroadcastPos();
        return new ValueTuple<List<Vector3>, List<double>>();
    }

    public void LoadMap(int mapId = 1)
    {
        var pathPrefix = Environment.GetEnvironmentVariable("MAP_DATA_PATH") ??
                            "/Users/jwy/Documents/Dev/CryWolf/Common/MapData";
        MinX = -100;
        MaxX = 100;
        MinZ = -240;
        MaxZ = 240;

        int xCount = MaxX - MinX + 1;
        int zCount = MaxZ - MinZ + 1;
        _collision = new bool[zCount, xCount];
        _objects = new GameObject[zCount, xCount];
        _objectsAir = new GameObject[zCount, xCount];
        
        // Collision 관련 파일
        var mapName = "Map_" + mapId.ToString("000");
        var txt = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
        var reader = new StringReader(txt);
        for (int z = 0; z < zCount; z++)
        {
            var line = reader.ReadLine();
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

    public Vector3 FindSpawnPos(GameObject gameObject)
    {
        GameObjectType type = gameObject.ObjectType;
        Vector3 cell = new Vector3();
        if (type != GameObjectType.Sheep) return new Vector3();
        
        bool canSpawn = false;
        while (canSpawn == false)
        {
            Random random = new();
            List<Vector3> xList = new List<Vector3>(GameData.SheepBounds);
            int minX = (int)(xList.Min(v => v.X) * _cellCnt);
            int maxX = (int)(xList.Max(v => v.X) * _cellCnt);
            int minZ = (int)(xList.Min(v => v.Z) * _cellCnt);
            int maxZ = (int)(xList.Max(v => v.Z) * _cellCnt);
            
            float x = (float)(random.Next(minX, maxX) * (double)1 / _cellCnt);
            float z = (float)(random.Next(minZ, maxZ) * (double)1 / _cellCnt);
            cell = new Vector3(x, 6, z);
            
            if (CanGo(gameObject, Vector3To2(cell), true, gameObject.Stat.SizeX))
                canSpawn = true;
        }

        Vector2Int vector = FindNearestEmptySpace(Vector3To2(cell), gameObject);
        Vector3 result = gameObject.UnitType == 0 ? Vector2To3(vector) : Vector2To3(vector, GameData.AirHeight);

        return result;
    }
}