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
    public bool CanGoGround(Vector3 cellPos, int unitSize = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)(MaxZ - cellPos.Z * 4);
        int cnt = 0;
        for (int i = x - (unitSize - 1); i <= x + (unitSize - 1); i++)
        {
            for (int j = z - (unitSize - 1); j <= z - (unitSize - 1); j++)
            {
                if (_collisionGround[j, i]) cnt++;
            }
        }

        return cnt == 0;
    }

    public bool CanGoAir(Vector3 cellPos, int unitSize = 1)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)(MaxZ - cellPos.Z * 4);
        int cnt = 0;
        for (int i = x - (unitSize - 1); i <= x + (unitSize - 1); i++)
        {
            for (int j = z - (unitSize - 1); j <= z - (unitSize - 1); j++)
            {
                if (_collisionGround[j, i]) cnt++;
            }
        }

        return cnt == 0;
    }

    public GameObject? Find(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return null;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return null;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)(MaxZ - cellPos.Z * 4);
        return _objects[z, x];
    }

    public bool ApplyLeave(GameObject gameObject)
    {
        PositionInfo posInfo = gameObject.PosInfo;
        if (posInfo.PosX < MinX || posInfo.PosX > MaxX) return false;
        if (posInfo.PosZ < MinZ || posInfo.PosZ > MaxZ) return false;

        int x = (int)((posInfo.PosX - MinX) * 4);
        int z = (int)(MaxZ - posInfo.PosZ * 4);
        if (_objects[z, x] == gameObject) _objects[z, x] = null;
        
        return true;
    }

    public List<Vector3> ApplyMove(GameObject gameObject, Vector3 startCell, Vector3 destCell)
    {
        int startRegionId = GetRegionByVector(Cell2Pos(startCell));
        int destRegionId = GetRegionByVector(Cell2Pos(destCell));
        List<int> regionPath = RegionPath(startRegionId, destRegionId);
        List<Vector3> center = new List<Vector3>();
        for (int i = 0; i < regionPath.Count; i++) center.Add(Pos2Cell(_regionGraph[i].CenterPos));
        List<Vector3> path = new List<Vector3>();
        Vector3 start = startCell;

        if (regionPath.Count == 0)
        {
            path = FindPath(startCell, destCell);
        }
        else
        {
            for (int i = 0; i < center.Count; i++)
            {
                List<Vector3> aStar = FindPath(start, center[i]);
                path.AddRange(aStar);
                start = path.Last();
            }

            List<Vector3> lastPath = FindPath(center.Last(), destCell);
            path.AddRange(lastPath);
        }

        return path;
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
}