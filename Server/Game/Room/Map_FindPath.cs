using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;

namespace Server.Game;

public struct PQNode : IComparable<PQNode>
{
    public double F;
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

public partial class Map
{
    public float MinX = -100;
    public float MaxX = 100;
    public float MinZ = -100;
    public float MaxZ = 100;
    public float GroundY = 6f;
    public float AirY = 8.5f;
    
    public int SizeX => (int)((MaxX - MinX) * 4 + 1); // grid = 0.25
    public int SizeZ => (int)((MaxZ - MinZ) * 4 + 1);
    
    private bool[,] _collisionGround;
    private bool[,] _collisionAir;
    private GameObject?[,] _objectsGround;
    private GameObject?[,] _objectsAir;
    private ushort[,] _objectPlayer;

    private int _regionIdGenerator = 0;
    private List<Region> _regionGraph = new();
    private int[,] _connectionMatrix;

    private int[,] _dist;
    private int[,] _parents;
    
    #region RegionDirection

    private static List<Vector3> _west = new()
    {
        new Vector3(-62, 0, -16),
        new Vector3(-19, 0, -16),
        new Vector3(-19, 0, 20),
        new Vector3(-62, 0, 20)
    };

    private static List<Vector3> _south = new()
    {
        new Vector3(-13, 0, -16),
        new Vector3(-13, 0, -21),
        new Vector3(13, 0, -21),
        new Vector3(13, 0, -16)
    };

    private static List<Vector3> _east = new()
    {
        new Vector3(15, 0, 10),
        new Vector3(15, 0, -16),
        new Vector3(54, 0, -16),
        new Vector3(54, 0, 10)
    };

    private static List<Vector3> _north = new()
    {
        new Vector3(-19, 0, 50),
        new Vector3(-19, 0, 20),
        new Vector3(15, 0, 20),
        new Vector3(15, 0, 50)
    };

    #endregion
    
    public void MapSetting()
    {
        DivideRegionMid(1);
        DivideRegion(_east, 6f);
        DivideRegion(_west, 6f);
        DivideRegion(_south, 6f);
        DivideRegion(_north, 6f);
        _connectionMatrix = RegionConnectivity();

        int cnt = _regionGraph.Count;
        _dist = new int[cnt, cnt];
        _parents = new int[cnt, cnt];

        for (int i = 0; i < _regionGraph.Count; i++) Dijkstra(i);
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
        int centerZ = (minZ + maxZ) / 2;
        int centerX = (minX + maxX) / 2;

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

    public Pos FindNearestEmptySpace(Pos pos, GameObject gameObject, int sizeZ = 1, int sizeX = 1)
    {
        int cnt = 0;
        int move = 0;
        sizeZ--;
        sizeX--;

        do
        {
            for (int i = -move; i <= move; i++)
            {
                pos.Z += i;
                for (int j = -move; j <= move; j++)
                {
                    pos.X += j;
                    for (int k = pos.Z - sizeZ; k <= pos.Z + sizeZ; k++)
                    {
                        for (int l = pos.X - sizeX; l <= pos.X + sizeX; l++)
                        {
                            if (gameObject.Stat.UnitType == 1) // 1 = air
                            {
                                if (_collisionAir[k, l]) cnt++;
                            }
                            else
                            {
                                if (_collisionGround[k, l]) cnt++;
                            }
                        }
                    }
                    if (cnt == 0) return pos;
                    cnt = 0;
                }
            }
            
            move++;
        } while (true);
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

        for (float i = minZ; i < maxZ - remainZ; i += sideZ)
        {
            for (float j = minX; j < maxX - remainX; j += sideX)
            {
                int lenZ = sideZ;
                int lenX = sideX;
                if (i + 2 * sideZ > maxZ) lenZ += remainZ;
                if (j + 2 * sideX > maxX) lenX += remainX;

                List<Pos> vertices = new List<Pos>
                {
                    Cell2Pos(new Vector3(j, 0, i)),
                    Cell2Pos(new Vector3(j, 0, i + lenZ)),
                    Cell2Pos(new Vector3(j + lenX, 0, i + lenZ)),
                    Cell2Pos(new Vector3(j + lenX, 0, i))
                };
                
                SortPointsCcw(vertices);
                
                Region newRegion = new()
                {
                    Id = _regionIdGenerator,
                    CenterPos = FindCenter(vertices),
                    Neighbor = new List<int>(),
                    Vertices = vertices,
                };
                
                _regionGraph.Add(newRegion);
                _regionIdGenerator++;
            }
        }
    }

    public void DivideRegionMid(int level)
    {
        List<Pos> vertices = new List<Pos>
        {
            Cell2Pos(new Vector3(-19, 0, 20)),
            Cell2Pos(new Vector3(15, 0, 20)),
            Cell2Pos(new Vector3(-19, 0, -16)),
            Cell2Pos(new Vector3(15, 0, -16)),
        };
        
        SortPointsCcw(vertices);
        Region newRegion = new Region
        {
            Id = _regionIdGenerator,
            CenterPos = FindCenter(vertices),
            Neighbor = new List<int>(),
            Vertices = vertices,
        };
        
        _regionGraph.Add(newRegion);
        _regionIdGenerator++;
    }

    private int[,] RegionConnectivity()
    {
        int[,] connectionMatrix = new int[_regionGraph.Count, _regionGraph.Count];
        
        for (int i = 0; i < _regionGraph.Count; i++)
        {
            List<Pos> regionOrigin = _regionGraph[i].Vertices;
            float xMinOrigin = regionOrigin.Min(v => v.X);
            float xMaxOrigin = regionOrigin.Max(v => v.X);
            float zMinOrigin = regionOrigin.Min(v => v.Z);
            float zMaxOrigin = regionOrigin.Max(v => v.Z);
            
            for (int j = 0; j < _regionGraph.Count; j++)
            {
                List<Pos> regionCompare = _regionGraph[j].Vertices;
                List<int> zOrigin = new List<int>();
                List<int> xOrigin = new List<int>();
                List<int> zCompared = new List<int>();
                List<int> xCompared = new List<int>();

                if (i < j)
                {
                    foreach (var p in regionOrigin)
                    {
                        zOrigin.Add(p.Z);
                        xOrigin.Add(p.X);
                    }

                    foreach (var p in regionCompare)
                    {
                        zCompared.Add(p.Z);
                        xCompared.Add(p.X);
                    }

                    List<int> zIntersection = zOrigin.Intersect(zCompared).ToList();
                    List<int> xIntersection = xOrigin.Intersect(xCompared).ToList();
                    bool adjacent = false;

                    if (zIntersection.Count != 0 && i != j)
                    {
                        for (int k = 0; k < regionCompare.Count; k++)
                        {
                            if (regionCompare[k].X > xMinOrigin && regionCompare[k].X < xMaxOrigin)
                                adjacent = true;
                        }
                    }
                    else if (xIntersection.Count != 0 && i != j)
                    {
                        for (int k = 0; k < regionCompare.Count; k++)
                        {
                            if (regionCompare[k].Z > zMinOrigin && regionCompare[k].Z < zMaxOrigin)
                                adjacent = true;
                        }
                    }
                    else
                    {
                        connectionMatrix[i, j] = -1;
                        continue;
                    }

                    if (adjacent)
                    {
                        connectionMatrix[i, j] = 10;
                    }
                    else
                    {
                        List<Pos> vIntersection = regionOrigin.Intersect(_regionGraph[j].Vertices).ToList();
                        connectionMatrix[i, j] = vIntersection.Count switch { 1 => 14, > 1 => 10, _ => -1 };
                    }
                }
                else
                {
                    connectionMatrix[i, j] = connectionMatrix[j, i];
                }
                
                if (connectionMatrix[i, j] != -1)
                {
                    _regionGraph[i].Neighbor.Add(_regionGraph[j].Id);
                }
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
        
        for (int i = 0; i < distance.Length; i++) _dist[start, i] = distance[i];
        for (int i = 0; i < parent.Length; i++) _parents[start, i] = parent[i];
    }
    
    public List<int> RegionPath(int startRegionId, int destRegionId)
    {
        List<int> path = new List<int>();
        int id = destRegionId;
        
        while (_parents[startRegionId, id] != startRegionId)
        {
            path.Add(_parents[startRegionId, id]);
            id = _parents[startRegionId, id];
        }
        path.Reverse();
        
        return path;
    }
    
    public Pos Cell2Pos(Vector3 cell)
    {
        // CellPos -> ArrayPos
        return new Pos((int)(MaxZ - cell.Z) * 4, (int)(cell.X - MinX) * 4);
    }

    public Vector3 Pos2Cell(Pos pos)
    {
        // ArrayPos -> CellPos
        return new Vector3(pos.X * 0.25f + MinX, 6, MaxZ - pos.Z * 0.25f);
    }
    
    public Vector3 Pos2CellAir(Pos pos)
    {
        // ArrayPos -> CellPos
        return new Vector3(pos.X * 0.25f + MinX, 9, MaxZ - pos.Z * 0.25f);
    }

    private int GetSideLen(float len, float minSide)
    {
        return len / 2 < minSide ? (int)len : GetSideLen(len / 2, minSide);
    }
    
    private int GetRegionByVector(Pos pos)
    {
        int crosses = 0;
       
        foreach (var region in _regionGraph)
        {
            List<Pos> v = region.Vertices;
            for (int i = 0; i < v.Count; i++)
            {
                int j = (i + 1) % v.Count;
                if (v[i].X > pos.X == v[j].X > pos.X) continue;
                double meetZ = (v[j].Z - v[i].Z) * (pos.X - v[i].X) / (v[j].X - v[i].X) + v[i].Z;
                if (pos.Z < meetZ) crosses++;
            }
        
            if (crosses % 2 > 0) return region.Id;
        }

        return int.MaxValue;
    }

    private static int[] _deltaZ = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static int[] _deltaX = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static int[] _cost = { 10, 14, 10, 14, 10, 14, 10, 14 };

    public List<Vector3> FindPath(GameObject gameObject, Vector3 startCellPos, Vector3 destCellPos,
        bool checkObjects = true)
    {
        bool[,] closed = new bool[SizeZ, SizeX];
        double[,] open = new double[SizeZ, SizeX];

        for (int z = 0; z < SizeZ; z++)
        {
            for (int x = 0; x < SizeX; x++) open[z, x] = Int32.MaxValue;
        }

        Pos[,] parent = new Pos[SizeZ, SizeX];
        PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();
        Pos pos = Cell2Pos(startCellPos);
        Pos dest = Cell2Pos(destCellPos);

        double hVal =  Math.Sqrt(Math.Pow(dest.Z - pos.Z, 2) + Math.Pow(dest.X - pos.X, 2));
        open[pos.Z, pos.X] = hVal;
        pq.Push(new PQNode { F = -hVal, G = 0, Z = pos.Z, X = pos.X});
        parent[pos.Z, pos.X] = new Pos(pos.Z, pos.X);

        while (pq.Count > 0)
        {
            PQNode node = pq.Pop();

            if (closed[node.Z, node.X]) continue;
            closed[node.Z, node.X] = true;
            
            if (node.Z == dest.Z && node.X == dest.X) break;

            for (int i = 0; i < _deltaZ.Length; i++)
            {
                Pos next = new Pos(node.Z + _deltaZ[i], node.X + _deltaX[i]);
                if (next.Z != dest.Z || next.X != dest.X)
                {
                    // if (CanGoGround(Pos2Cell(next), checkObjects) == false, gameObject.Stat.SizeX, gameObject.Stat.SizeZ)
                    if (CanGoGround(Pos2Cell(next), checkObjects) == false)
                        continue;
                }

                if (closed[next.Z, next.X]) continue;

                int g = node.G + _cost[i];
                int h = 10 * ((dest.Z - next.Z) * (dest.Z - next.Z) + (dest.X - next.X) * (dest.X - next.X));
                if (open[next.Z, next.X] < g + h) continue;
                
                open[dest.Z, dest.X] = g + h;
                pq.Push(new PQNode { F = -(g + h), G = g, Z = next.Z, X = next.X });
                parent[next.Z, next.X] = new Pos(node.Z, node.X);
            }
        }

        return CalcCellPathFromParent(parent, dest);
    }
    
    private List<Vector3> CalcCellPathFromParent(Pos[,] parent, Pos dest)
    {
        List<Vector3> cells = new List<Vector3>();

        int z = dest.Z;
        int x = dest.X;
        while (parent[z, x].Z != z || parent[z, x].X != x)
        {
            cells.Add(Pos2Cell(new Pos(z, x)));
            Pos pos = parent[z, x];
            z = pos.Z;
            x = pos.X;
        }

        cells.Add(Pos2Cell(new Pos(z, x)));
        cells.Reverse();

        return cells;
    }

    public Vector3 GetClosestPoint(Vector3 cellPos, GameObject target)
    {
        Vector3 targetCellPos = target.CellPos;
        Vector3 destVector;
        
        int sizeX = target.Stat.SizeX;
        int sizeZ = target.Stat.SizeZ;
        
        double deltaX = cellPos.X - targetCellPos.X; // P2 cellPos , P1 targetCellPos
        double deltaZ = cellPos.Z - targetCellPos.Z;
        double theta = Math.Round(Math.Atan2(deltaZ, deltaX) * (180 / Math.PI), 2);
        double x;
        double z;

        if (deltaX != 0)
        {
            double slope = deltaZ / deltaX;
            double zIntercept = targetCellPos.Z - slope * targetCellPos.X;

            switch (theta)
            {
                case >= 45 and <= 135:          // up
                    z = targetCellPos.Z + sizeZ;
                    x = (z - zIntercept) / slope;
                    destVector = Pos2Cell(Cell2Pos(new Vector3((float)x, 6, (float)z))); // 0.27 이런좌표를 0.25로 변환 
                    break;
                case <= -45 and >= -135:        // down
                    z = targetCellPos.Z - sizeZ;
                    x = (z - zIntercept) / slope;
                    destVector = Pos2Cell(Cell2Pos(new Vector3((float)x, 6, (float)z)));
                    break;
                case > -45 and < 45:            // right
                    x = targetCellPos.X + sizeX;
                    z = slope * x - zIntercept;
                    destVector = Pos2Cell(Cell2Pos(new Vector3((float)x, 6, (float)z)));
                    break;
                default:                        // left
                    x = targetCellPos.X - sizeX;
                    z = slope * x - zIntercept;
                    destVector = Pos2Cell(Cell2Pos(new Vector3((float)x, 6, (float)z)));
                    break;
            }
        }
        else
        {
            switch (theta)
            {
                case >= 45 and <= 135:          // up
                    z = targetCellPos.Z + sizeZ;
                    destVector = Pos2Cell(Cell2Pos(new Vector3(target.CellPos.X, 6, (float)z))); 
                    break;
                default:
                    z = targetCellPos.Z - sizeZ;
                    destVector = Pos2Cell(Cell2Pos(new Vector3(target.CellPos.X, 6, (float)z))); 
                    break;
            }
        }

        return destVector;
    }
}