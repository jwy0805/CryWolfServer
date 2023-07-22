using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public struct Pos
{
    public Pos(int z, int x) { Z = z; X = x; }
    public int Z;
    public int X;
}

public partial class Map
{
    public bool CanGoGround(Vector3 cellPos, bool checkObjects = true, int size = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)((MaxZ - cellPos.Z) * 4);
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z + (size - 1); j++)
            {
                if (_collisionGround[j, i]) cnt++;
            }
        }

        return cnt == 0 && (!checkObjects || _objectsGround[z, x] == null);
    }

    public bool CanGoAir(Vector3 cellPos, bool checkObjects = true, int size = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)((MaxZ - cellPos.Z) * 4);
        int cnt = 0;
        for (int i = x - (size - 1); i <= x + (size - 1); i++)
        {
            for (int j = z - (size - 1); j <= z - (size - 1); j++)
            {
                if (_collisionAir[j, i]) cnt++;
            }
        }

        return cnt == 0;
    }

    public GameObject? Find(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return null;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return null;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)((MaxZ - cellPos.Z) * 4);
        return _objectsGround[z, x];
    }

    public bool ApplyLeave(GameObject gameObject)
    {
        PositionInfo posInfo = gameObject.PosInfo;
        StatInfo stat = gameObject.Stat;
        if (posInfo.PosX < MinX || posInfo.PosX > MaxX) return false;
        if (posInfo.PosZ < MinZ || posInfo.PosZ > MaxZ) return false;

        int x = (int)((posInfo.PosX - MinX) * 4);
        int z = (int)((MaxZ - posInfo.PosZ) * 4);
        int xSize = stat.SizeX;
        int zSize = stat.SizeZ;
        
        for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
        {
            for (int j = z - (zSize - 1); j <= z - (zSize - 1); j++)
            {
                switch (stat.UnitType)
                {
                    case 0: // 0 -> ground
                        _objectsGround[z, x] = null;
                        break;
                    case 1: // 1 -> air
                        _objectsAir[z, x] = null;
                        break;
                    case 2: // 2 -> player
                        _objectPlayer[z, x] = 0;
                        break;
                }
            }
        }

        return true;
    }

    public bool ApplyMap(GameObject gameObject)
    {
        ApplyLeave(gameObject);
        if (gameObject.Room == null) return false;
        if (gameObject.Room.Map != this) return false;

        PositionInfo posInfo = gameObject.PosInfo;
        StatInfo stat = gameObject.Stat;
        
        int x = (int)((gameObject.CellPos.X - MinX) * 4);
        int z = (int)((MaxZ - gameObject.CellPos.X) * 4);
        int xSize = stat.SizeX;
        int zSize = stat.SizeZ;
        
        for (int i = x - (xSize - 1); i <= x + (xSize - 1); i++)
        {
            for (int j = z - (zSize - 1); j <= z + (zSize - 1); j++)
            {
                switch (stat.UnitType)
                {
                    case 0: // 0 -> ground
                        _objectsGround[z, x] = gameObject;
                        break;
                    case 1: // 1 -> air
                        _objectsAir[z, x] = gameObject;
                        break;
                    case 2: // 2 -> player
                        _objectPlayer[z, x] = 1;
                        break;
                }
            }
        }
        
        return true;
    }
    
    public (List<Vector3>, List<double>) Move(GameObject gameObject, Vector3 startCell, Vector3 destCell)
    {
        int startRegionId = GetRegionByVector(Cell2Pos(startCell));
        int destRegionId = GetRegionByVector(Cell2Pos(destCell));
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        List<Vector3> center = new();
        for (int i = 0; i < regionPath.Count; i++) center.Add(Pos2Cell(_regionGraph[regionPath[i]].CenterPos));
        List<Vector3> path = new();
        List<double> arctan = new();
        Vector3 start = startCell;

        if (regionPath.Count == 0)
        {
            path = FindPath(gameObject, startCell, destCell);
        }
        else
        {
            for (int i = 0; i < center.Count; i++)
            {
                List<Vector3> aStar = FindPath(gameObject, start, center[i]);
                path.AddRange(aStar);
                start = path.Last();
            }

            List<Vector3> lastPath = FindPath(gameObject, center.Last(), destCell);
            path.AddRange(lastPath);
        }

        HashSet<Vector3> set = new HashSet<Vector3>(path);
        List<Vector3> uniquePath = set.ToList();

        for (int i = 0; i < uniquePath.Count; i++)
        {
            double atan2 = i == 0 ?
                Math.Round(Math.Atan2(uniquePath[i].X - startCell.X, uniquePath[i].Z - startCell.Z)):
                Math.Round(Math.Atan2(uniquePath[i].X - uniquePath[i - 1].X, uniquePath[i].Z - uniquePath[i - 1].Z) * (180 / Math.PI), 2);
            arctan.Add(atan2);
        }
        
        return (uniquePath, arctan);
    }

    public void LoadMap(int mapId = 1, string pathPrefix = "/Users/jwy/Documents/dev/CryWolf/Common/MapData")
    {
        string mapName = "Map_" + mapId.ToString("000");
        
        // Collision 관련 파일
        string txt = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
        StringReader reader = new StringReader(txt);
        
        int xCount = (int)((MaxX - MinX) * 4 + 1);
        int zCount = (int)((MaxZ - MinZ) * 4 + 1);
        _collisionGround = new bool[zCount, xCount];
        _collisionAir = new bool[zCount, xCount];
        _objectsGround = new GameObject[zCount, xCount];
        _objectsAir = new GameObject[zCount, xCount];
        _objectPlayer = new ushort[zCount, xCount];

        for (int z = 0; z < zCount; z++)
        {
            string? line = reader.ReadLine();
            for (int x = 0; x < xCount; x++)
            {
                if (line != null)
                {
                    _collisionGround[z, x] = line[x] == '2' || line[x] == '4';
                    _collisionAir[z, x] = line[x] == '4';
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
            cell = new Vector3(0, 0, 0);
        }

        Pos pos = FindNearestEmptySpace(Cell2Pos(cell), gameObject, gameObject.Stat.SizeX, gameObject.Stat.SizeX);
        Vector3 result = Pos2Cell(pos);

        return result;
    }
}