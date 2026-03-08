using System.Collections;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class Map
{
    private readonly List<(int, int)> _coordBuffer = new(64);

    public readonly short CellCnt = 4; // Vector3 좌표 1당 vector2 좌표 4개
    public GameManager.GameData? GameData { get; set; }
    public GameRoom? Room { get; set; }

    public bool ApplyMap(GameObject gameObject, Vector3 pos = new(), bool checkObjects = true)
    {
        ApplyLeave(gameObject);
        if (gameObject.Room == null) return false;
        if (gameObject.Room.Map != this) return false;

        StatInfo stat = gameObject.Stat;
        Vector2Int v =
            Vector3To2(new Vector3(gameObject.PosInfo.PosX, gameObject.PosInfo.PosY, gameObject.PosInfo.PosZ));

        if (gameObject.ObjectType != GameObjectType.Fence)
        {
            if (!CanGo(gameObject, v, checkObjects))
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
        Vector3 cellPos = new Vector3(gameObject.PosInfo.PosX, gameObject.PosInfo.PosY, gameObject.PosInfo.PosZ);
        Vector2Int v2 = Vector3To2(cellPos);
        int halfX = gameObject.Stat.SizeX - 1;
        int halfZ = gameObject.Stat.SizeZ - 1;
        if (v2.X - halfX < _minX || v2.X + halfX > _maxX) return false;
        if (v2.Z - halfZ < _minZ || v2.Z + halfZ > _maxZ) return false;

        List<(int, int)> coordinates = CalculateCoordinates(posInfo, stat);

        switch (stat.UnitType)
        {
            case 0: // 0 -> ground
                ClearObjects(gameObject, coordinates, _objects);
                break;
            case 1: // 1 -> air
                ClearObjects(gameObject, coordinates, _objectsAir);
                break;
            case 2: // 2 -> player
                foreach (var (i, j) in coordinates)
                {
                    if ((uint)i >= (uint)_objectPlayer.GetLength(0) ||
                        (uint)j >= (uint)_objectPlayer.GetLength(1)) 
                        continue;

                    _objectPlayer[i, j] = 0;
                }
                break;
        }

        return true;
    }

    private List<(int, int)> CalculateCoordinates(PositionInfo posInfo, StatInfo stat)
    {
        int centerX = (int)(posInfo.PosX * CellCnt - _minX);
        int centerZ = (int)(_maxZ - posInfo.PosZ * CellCnt);
        bool isVertical = posInfo.Dir is > 45 and < 135 or > 225 and < 315;
        int radiusX = (isVertical ? stat.SizeZ : stat.SizeX) - 1;
        int radiusZ = (isVertical ? stat.SizeX : stat.SizeZ) - 1;
        int x0 = Math.Max(0, centerX - radiusX);
        int x1 = Math.Min(_sizeX - 1, centerX + radiusX);
        int z0 = Math.Max(0, centerZ - radiusZ);
        int z1 = Math.Min(_sizeZ - 1, centerZ + radiusZ);
        List<(int, int)> coordinates = _coordBuffer;
        coordinates.Clear();

        for (int z = z0; z < z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                coordinates.Add((z, x)); // row z, col x
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

    private void ClearObjects(GameObject gameObject, List<(int, int)> coordinates, GameObject?[,] grid)
    {
        foreach (var (i, j) in coordinates)
        {
            grid[i, j] = null;
        }
    }

    public bool CanSpawnFence(Vector2Int cellPos, Tower[] towers)
    {
        if (cellPos.X < _minX || cellPos.X > _maxX) return false;
        if (cellPos.Z < _minZ || cellPos.Z > _maxZ) return false;

        Pos pos = Cell2Pos(cellPos);
        int x = pos.X;
        int z = pos.Z;

        const int half = 4; // 울타리 크기 9칸
        if ((uint)z >= (uint)_sizeZ) return false;
        if (x - half < 0 || x + half >= _sizeX) return false;

        for (int i = x - half; i <= x + half; i++)
        {
            if (_collision[z, i]) return false;
            if (_objects[z, i] != null) return false;
        }

        var fenceCell = Vector2To3(cellPos);
        for (int i = 0; i < towers.Length; i++)
        {
            Tower t = towers[i];
            if (fenceCell.Z >= t.CellPos.Z - (t.SizeZ - 1) && fenceCell.Z <= t.CellPos.Z + (t.SizeZ - 1) &&
                fenceCell.X >= t.CellPos.X - (t.SizeX - 1) && fenceCell.X <= t.CellPos.X + (t.SizeX - 1))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanGo(GameObject go, Vector2Int cellPos, bool checkObjects = true)
    {
        int halfX = go.SizeX - 1;
        int halfZ = go.SizeZ - 1;
        if (cellPos.X - halfX < _minX || cellPos.X + halfX > _maxX) return false;
        if (cellPos.Z - halfZ < _minZ || cellPos.Z + halfZ > _maxZ) return false;

        GameObject?[,] objects = go.UnitType == 0 ? _objects : _objectsAir;
        Pos pos = Cell2Pos(cellPos);
        int x = pos.X;
        int z = pos.Z;
        int selfId = go.Id;
        int targetId = go.Target?.Id ?? 0;
        
        for (int i = x - halfX; i <= x + halfX; i++)
        {
            for (int j = z - halfZ; j <= z + halfZ; j++)
            {
                if (_collision[j, i]) return false;
                if (!checkObjects) continue;
                
                var obj = objects[j, i];
                if (obj == null) continue;

                int objId = obj.Id;
                if (objId == selfId || objId == targetId) continue;

                return false;
            }
        }

        return true;
    } 

    public void MoveAlongPath(GameObject go, List<Vector3> outPath, List<double> outAtan)
    {
        outAtan.Clear();
        if (outPath.Count == 0) return;

        for (int i = 0; i < outPath.Count - 1; i++)
        {
            double xDiff = outPath[i + 1].X - outPath[i].X;
            double zDiff = outPath[i + 1].Z - outPath[i].Z;
            double atan2 = Math.Round(Math.Atan2(xDiff, zDiff) * (180 / Math.PI), 2);
            outAtan.Add(atan2);
        }
        if (outAtan.Count > 0) outAtan.Add(outAtan[^1]);
        
        int moveTick = (int)(go.TotalMoveSpeed * CellCnt * go.CallCycle / 1000 * 100 + go.DistRemainder);
        int index = 0;

        while (moveTick >= 100 && index + 1 < outPath.Count)
        {
            double xDiff = outPath[index + 1].X - outPath[index].X;
            double zDiff = outPath[index + 1].Z - outPath[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;
            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }
        
        go.DistRemainder = moveTick;
        index = Math.Min(index, outPath.Count - 1);
        ApplyMap(go, outPath[index]);
    }
    
    public bool TryKnockBack(
        GameObject go, List<Vector3> outPath, bool checkObjects = false)
    {
        outPath.Clear();
        
        Vector2Int startCell = Vector3To2(go.CellPos);
        Vector2Int destCell = Vector3To2(go.DestPos);
        if (!TryFindPath(go, startCell, destCell, outPath, 0, false, checkObjects)) return false;
        
        RemoveDuplicatedPaths(outPath);
        if (outPath.Count == 0) return false;
        
        int moveTick = (int)((go.TotalMoveSpeed * CellCnt * go.CallCycle / 1000 + go.DistRemainder) * 100);
        int index = 0;
        while (moveTick >= 100 && index + 1 < outPath.Count)
        {
            double xDiff = outPath[index + 1].X - outPath[index].X;
            double zDiff = outPath[index + 1].Z - outPath[index].Z;
            int cost = (xDiff == 0 || zDiff == 0) ? 100 : 140;

            if (moveTick >= cost)
            {
                moveTick -= cost;
                index++;
            }
            else break;
        }
        
        go.DistRemainder = moveTick;
        index = Math.Min(index, outPath.Count - 1);
        Vector2Int stepCell = Vector3To2(outPath[index]);
        if (!CanGo(go, stepCell, true))
        {
            // 이동 중 충돌 발생 -> IDLE로 상태 변화
            ApplyMap(go, go.CellPos);
            go.State = State.Idle;
            outPath.Clear();
            return false;
        }
        
        ApplyMap(go, outPath[index]);

        return true;
    }

    public bool TryGetPath(
        GameObject attacker, int range, GameObject target, List<Vector3> outPath, bool checkObjects = true)
    {
        outPath.Clear();
        
        Vector2Int startCell = Vector3To2(attacker.CellPos);
        Vector2Int destCell = Vector3To2(target.CellPos);
        if (!CanGo(attacker, destCell, checkObjects))
        {
            destCell = FindNearestEmptySpace(destCell, attacker);
        }
        
        int startRegionId = GetRegionByVector(startCell);
        int destRegionId = GetRegionByVector(destCell);
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        
        // Path 추출
        Vector2Int destCellVector = destCell;
        bool isHop = false;
        if (regionPath.Count > 1)
        {
            destCellVector = GetCenter(regionPath[0], attacker, startCell, destCell);
            isHop = true;
        }

        bool ok = isHop 
            ? TryFindPath(attacker, startCell, destCellVector, outPath, 0, false, checkObjects) 
            : TryFindPath(attacker, startCell, destCellVector, outPath, range, true, checkObjects);
        if (!ok)
        {
            outPath.Clear();
            return false;
        }
        
        RemoveDuplicatedPaths(outPath);
        
        return outPath.Count > 0;
    }
    
    public void RemoveDuplicatedPaths(List<Vector3> path)
    {
        if (path.Count <= 1) return;

        int write = 1;
        Vector3 prev = path[0];
        for (int read = 1; read < path.Count; read++)
        {
            if (path[read] == prev) continue;
            prev = path[read];
            path[write++] = prev;
        }
        
        if (write < path.Count)
        {
            path.RemoveRange(write, path.Count - write);
        }
    }

    public void LoadMap(int mapId = 1)
    {
        var pathPrefix = Environment.GetEnvironmentVariable("Map__Path") ?? 
                         "/Users/jwy/Documents/00_Dev/00_CryWolf/Common/MapData";
        _minX = -12 * CellCnt;
        _maxX = 12 * CellCnt;
        _minZ = -20 * CellCnt;
        _maxZ = 20 * CellCnt;
        _sizeX = _maxX - _minX + 1;
        _sizeZ = _maxZ - _minZ + 1;
        
        _collision = new bool[_sizeZ, _sizeX];
        _objects = new GameObject[_sizeZ, _sizeX];
        _objectsAir = new GameObject[_sizeZ, _sizeX];
        _visited = new int[_sizeZ, _sizeX];
        
        // Collision 관련 파일
        var mapName = "Map_" + mapId.ToString("000");
        var txt = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
        var reader = new StringReader(txt);
        for (int z = 0; z < _sizeZ; z++)
        {
            var line = reader.ReadLine();
            for (int x = 0; x < _sizeX; x++)
            {
                if (line != null)
                {
                    _collision[z, x] = line[x] == '2' || line[x] == '4';
                    // _collisionAir[z, x] = line[x] == '4';
                }
            }
        }
    }

    public Vector3 FindSheepSpawnPos(GameObject gameObject)
    {
        if (Room == null) return new Vector3();
        
        GameObjectType type = gameObject.ObjectType;
        Vector3 cell = new Vector3();
        if (type != GameObjectType.Sheep) return new Vector3();
        
        bool canSpawn = false;
        while (!canSpawn)
        {
            Random random = new();
            List<Vector3> xList = new List<Vector3>(Room.GetSheepBounds());
            int minX = (int)(xList.Min(v => v.X) * CellCnt);
            int maxX = (int)(xList.Max(v => v.X) * CellCnt);
            int minZ = (int)(xList.Min(v => v.Z) * CellCnt);
            int maxZ = (int)(xList.Max(v => v.Z) * CellCnt);
            
            float x = (float)(random.Next(minX, maxX) * (double)1 / CellCnt);
            float z = (float)(random.Next(minZ, maxZ) * (double)1 / CellCnt);
            cell = new Vector3(x, 6, z);
            
            if (CanGo(gameObject, Vector3To2(cell)))
                canSpawn = true;
        }

        Vector2Int vector = FindNearestEmptySpace(Vector3To2(cell), gameObject);
        Vector3 result = gameObject.UnitType == 0 ? Vector2To3(vector) : Vector2To3(vector, GameData!.AirHeight);

        return result;
    }
}