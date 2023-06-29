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

public struct PQNode : IComparable<PQNode>
{
    public int F;
    public int G;
    public int Z;
    public int X;

    public int CompareTo(PQNode other)
    {
        if (F < other.F) return -1;
        return F > other.F ? 1 : 0;
    }
}

public struct Region
{
    public int Id;
    public Pos CenterPos;
    public List<int> Neighbor;
    public List<Pos> Vertices;

    public Region(int id)
    {
        Id = id;
        CenterPos = new Pos();
        Neighbor = new List<int>();
        Vertices = new List<Pos>();
    }
}

public class Map
{
    public float MinX = -100f;
    public float MaxX = 100f;
    public float MinZ = -100f;
    public float MaxZ = 100f;
    public float GroundY = 6f;
    public float AirY = 8.5f;
    
    public int SizeX => (int)((MaxX - MinX) * 4 + 1); // grid = 0.25
    public int SizeZ => (int)((MaxZ - MinZ) * 4 + 1);
    
    private bool[,] _collisionGround;
    private bool[,] _collisionAir;
    private GameObject?[,] _objects;

    private List<Region> _regionGraph = new();
    private int[,] _connectionMatrix;

    public bool CanGoGround(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)(MaxZ - cellPos.Z * 4);
        return !_collisionGround[z, x];
    }
    
    public bool CanGoAir(Vector3 cellPos)
    {
        if (cellPos.X < MinX || cellPos.X > MaxX) return false;
        if (cellPos.Z < MinZ || cellPos.Z > MaxZ) return false;

        int x = (int)((cellPos.X - MinX) * 4);
        int z = (int)(MaxZ - cellPos.Z * 4);
        return !_collisionAir[z, x];
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
    
    public void LoadMap(int mapId = 1, string pathPrefix = "")
    {
        string mapName = "Map_" + mapId.ToString("000");
        
        // Collision 관련 파일
        string txt = File.ReadAllText($"Map/{mapName}");
        StringReader reader = new StringReader(txt);
        
        int xCount = (int)((MaxX - MinX) * 4 + 1);
        int zCount = (int)((MaxZ - MinZ) * 4 + 1);
        _collisionGround = new bool[zCount, xCount];
        _collisionAir = new bool[zCount, xCount];

        for (int z = 0; z < zCount; z++)
        {
            string line = reader.ReadLine();
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
    
        private void SortPointsCcw(List<Pos> points)
    {
        float sumX = 0;
        float sumZ = 0;

        for (int i = 0; i < points.Count; i++)
        {
            sumX += points[i].X;
            sumZ += points[i].Z;
        }
        
        float averageX = sumX / points.Count;
        float averageZ = sumZ / points.Count;
        
        points.Sort((lhs, rhs) =>
        {
            double lhsAngle = Math.Atan2(lhs.Z - averageZ, lhs.X - averageX);
            double rhsAngle = Math.Atan2(rhs.Z - averageZ, rhs.X - averageX);
            
            if (lhsAngle < rhsAngle) return -1;
            if (lhsAngle > rhsAngle) return 1;
            double lhsDist = Math.Sqrt(Math.Pow(lhs.Z - averageZ, 2) + Math.Pow(lhs.X - averageX, 2));
            double rhsDist = Math.Sqrt(Math.Pow(rhs.Z - averageZ, 2) + Math.Pow(rhs.X - averageX, 2));
            if (lhsDist < rhsDist) return 1;
            if (lhsDist > rhsDist) return -1;
            return 0;
        });
    }
    
    private Pos FindCenter(List<Pos> vertices)
    {
        int minZ = vertices.Select(v => v.Z).ToList().Min();
        int maxZ = vertices.Select(v => v.Z).ToList().Max();
        int minX = vertices.Select(v => v.X).ToList().Min();
        int maxX = vertices.Select(v => v.X).ToList().Max();
        int centerZ = minZ + maxZ / 2;
        int centerX = minX + maxX / 2;

        int size = 1;
        int startZ = centerZ;
        int startX = centerX;

        while (startZ >= minZ && startX >= minX && startZ < maxZ && startX < maxX)
        {
            for (int i = startZ - size; i <= startZ + size; i++)
            {
                for (int j = startX - size; j <= startX + size; j++)
                {
                    if (i >= 0 && i < maxZ && j >= 0 && j < maxX && _collisionGround[i, j] == false)
                    {
                        return new Pos { Z = i, X = j };
                    }
                }
            }

            size++;
            startZ = centerZ - size;
            startX = centerX - size;
        }

        return new Pos { Z = -1, X = -1 };
    }

    #region RegionDirection

    private List<Vector3> _west = new()
    {
        new Vector3(-62, 0, -16),
        new Vector3(-19, 0, -16),
        new Vector3(-19, 0, 20),
        new Vector3(-62, 0, 20)
    };

    private List<Vector3> _south = new()
    {
        new Vector3(-13, 0, -16),
        new Vector3(-13, 0, -21),
        new Vector3(13, 0, -21),
        new Vector3(13, 0, -16)
    };

    private List<Vector3> _east = new()
    {
        new Vector3(13, 0, 10),
        new Vector3(13, 0, -16),
        new Vector3(70, 0, -16),
        new Vector3(70, 0, 10),
    };

    private List<Vector3> _north = new()
    {
        new Vector3(-19, 0, 50),
        new Vector3(-19, 0, 20),
        new Vector3(15, 0, 20),
        new Vector3(15, 0, 50)
    };

    private List<Vector3> _mid = new()
    {
        new Vector3(-19, 0, 20),
        new Vector3(-19, 0, -16),
        new Vector3(19, 0, -16),
        new Vector3(19, 0, 20)
    };
    
    private int _idGenerator = 0;

    #endregion

    private int GetSideLen(float len, float minSide)
    {
        return len / 2 < minSide ? (int)len : GetSideLen(len / 2, minSide);
    }
    
    public void DivideRegion(List<Vector3> region, float lenSide)
    {
        float minX = region.Min(v => v.X);
        float maxX = region.Max(v => v.X);
        float minZ = region.Min(v => v.Z);
        float maxZ = region.Max(v => v.Z);

        int sideX = GetSideLen(maxX - minX, lenSide);
        int sideZ = GetSideLen(maxZ - minZ, lenSide);

        int remainX = (int)(maxX - minX) % sideX;
        int remainZ = (int)(maxZ - minZ) % sideZ;

        for (float i = minZ; i < maxZ; i += sideZ)
        {
            for (float j = minX; j < maxX; j += sideX)
            {
                if (i + 2 * sideZ > maxZ) i += remainZ;
                if (j + 2 * sideX > maxX) j += remainX;
                
                List<Pos> vertices = new List<Pos>
                {
                    Cell2Pos(new Vector3(j, 0, i)),
                    Cell2Pos(new Vector3(j, 0, i + sideZ)),
                    Cell2Pos(new Vector3(j + sideX, 0, i + sideZ)),
                    Cell2Pos(new Vector3(j + sideX, 0, i))
                };
                
                SortPointsCcw(vertices);
                
                Region newRegion = new()
                {
                    Id = _idGenerator,
                    CenterPos = FindCenter(vertices),
                    Vertices = vertices,
                };
                
                _regionGraph.Add(newRegion);
                _idGenerator++;
            }
        }
    }

    public void DivideRegionMid(int level)
    {
        Vector3[] fenceSize = GameData.FenceSize;
        Vector3[] fenceCenter = GameData.FenceCenter;
        float lenFenceX = fenceSize[level].X;
        float lenFenceZ = fenceSize[level].Z;
        List<Pos>[] verticesList = new List<Pos>[5];

        List<Pos> fenceVertices = new List<Pos>
        {
            Cell2Pos(new Vector3(fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z + lenFenceZ / 2)),
            Cell2Pos(new Vector3(fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z - lenFenceZ / 2)),
            Cell2Pos(new Vector3(fenceCenter[level].X + lenFenceX / 2, 0, fenceCenter[level].Z + lenFenceZ / 2)),
            Cell2Pos(new Vector3(fenceCenter[level].X + lenFenceX / 2, 0, fenceCenter[level].Z - lenFenceZ / 2))
        };

        List<Vector3> upper = new List<Vector3>
        {
            new (-19, 0, 20),
            new (-19, 0, fenceCenter[level].Z),
            new (fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z),
            new (fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z + lenFenceZ / 2),
            new (fenceCenter[level].X, 0, fenceCenter[level].Z + lenFenceZ / 2),
            new (fenceCenter[level].X, 0, 20)
        };

        List<Vector3> lower = new List<Vector3>
        {
            new(-19, 0, fenceCenter[level].Z),
            new(-19, 0, -16),
            new(fenceCenter[level].X, 0, -16),
            new(fenceCenter[level].X, 0, fenceCenter[level].Z - lenFenceZ / 2),
            new(fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z - lenFenceZ / 2),
            new(fenceCenter[level].X - lenFenceX / 2, 0, fenceCenter[level].Z),
        };


        for (int i = 0; i < verticesList.Length; i++)
        {
            verticesList[i] = new List<Pos>();
        }

        verticesList[0] = fenceVertices;
        
        foreach (var v in upper)
        {
            verticesList[1].Add(Cell2Pos(v));
            Vector3 vec = new Vector3(-v.X, v.Y, v.Z);
            verticesList[2].Add(Cell2Pos(vec));
        }
        
        foreach (var v in lower)
        {
            verticesList[3].Add(Cell2Pos(v));
            Vector3 vec = new Vector3(-v.X, v.Y, v.Z);
            verticesList[4].Add(Cell2Pos(vec));
        }

        foreach (var t in verticesList)
        {
            SortPointsCcw(t);
            Region newRegion = new Region()
            {
                Id = _idGenerator,
                CenterPos = FindCenter(t),
                Vertices = t,
            };

            _regionGraph.Add(newRegion);
            _idGenerator++;
        }
    }

    private int[,] RegionConnectivity()
    {
        int[,] connectionMatrix = new int[_regionGraph.Count, _regionGraph.Count];
        
        for (int i = 0; i < _regionGraph.Count; i++)
        {
            List<Pos> region = _regionGraph[i].Vertices;
            for (int j = 0; j < _regionGraph.Count; j++)
            {
                List<Pos> intersection = region.Intersect(_regionGraph[j].Vertices).ToList();
                if (region.SequenceEqual(intersection)) connectionMatrix[i, j] = 0;
                if (intersection.Count == 0)connectionMatrix[i, j] = -1;
                else connectionMatrix[i, j] = intersection.Count % 2 == 0 ? 10 : 14;
            }
        }

        return connectionMatrix;
    }

    public void Dijkstra(int start)
    {
        bool[] visited = new bool[_regionGraph.Count];
        int[] distance = new int[_regionGraph.Count];
        int[] parent = new int[_regionGraph.Count];
        Array.Fill(distance, Int32.MaxValue);

        distance[start] = 0;
        parent[start] = start;

        while (true)
        {
            int closest = Int32.MaxValue;
            int now = -1;
            for (int i = 0; i < _regionGraph.Count; i++)
            {
                if (visited[i]) continue;
                if (distance[i] == Int32.MaxValue || distance[i] >= closest) continue;
                
                closest = distance[i];
                now = i;
            }

            if (now == -1) break;
            visited[now] = true;

            for (int next = 0; next < _regionGraph.Count; next++)
            {
                if (_connectionMatrix[now, next] == -1) continue;
                if (visited[next]) continue;
                
                int nextDist = distance[now] + _connectionMatrix[now, next];
                if (nextDist < distance[next])
                {
                    distance[next] = nextDist;
                    parent[next] = now;
                }
            }
        }
    }
    
    Pos Cell2Pos(Vector3 cell)
    {
        // CellPos -> ArrayPos
        return new Pos((int)(MaxZ - cell.Z), (int)(cell.X - MinX));
    }

    Vector3 Pos2Cell(Pos pos)
    {
        // ArrayPos -> CellPos
        return new Vector3(pos.X+ MinX, 0,  MaxZ - pos.Z);
    }
}