using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;

namespace Server.Game;

public struct Pos
{
    public Pos(int z, int x) { Z = z; X = x; }
    public int Z;
    public int X;
    
    public static bool operator ==(Pos lhs, Pos rhs)
    {
        return lhs.Z == rhs.Z && lhs.X == rhs.X;
    }

    public static bool operator !=(Pos lhs, Pos rhs)
    {
        return !(lhs == rhs);
    }

    public override bool Equals(object obj)
    {
        return (Pos)obj == this;
    }

    public override int GetHashCode()
    {
        long value = (Z << 32) | X;
        return value.GetHashCode();
    }
}

public struct PQNode : IComparable<PQNode>
{
    public int F;
    public int G;
    public int Z;
    public int X;

    public int CompareTo(PQNode other)
    {
        if (F == other.F) return 0;
        return F < other.F ? 1 : -1;
    }
}

public struct Vector2Int
{
    public int X;
    public int Z;

    public Vector2Int(int x, int z)
    {
        X = x;
        Z = z;
    }

    public static Vector2Int operator +(Vector2Int v1, Vector2Int v2)
    {
        return new Vector2Int(v1.X + v2.X, v1.Z + v2.Z);
    }
    
    public static Vector2Int operator -(Vector2Int v1, Vector2Int v2)
    {
        return new Vector2Int(v1.X - v2.X, v1.Z - v2.Z);
    }

    public static double SignedAngle(Vector2Int startVec, Vector2Int destVec, float rotation)
    {
        double unsignedAngle = Math.Atan2(destVec.Z, destVec.X) * 180 / Math.PI - 
                               Math.Atan2(startVec.Z, startVec.X) * 180 / Math.PI;
        double sign = Math.Sign(startVec.X * destVec.Z - startVec.Z * destVec.X);
        unsignedAngle = (unsignedAngle + 180) % 360 - 180;
        unsignedAngle -= rotation;

        return unsignedAngle * sign;
    }

    public float Magnitude => (float)Math.Sqrt(SqrMagnitude);
    public int SqrMagnitude => X * X + Z * Z;
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
    public static int MinX { get; set; }
    public static int MaxX { get; set; }
    public static int MinZ { get; set; }
    public static int MaxZ { get; set; }

    public int SizeX => MaxX - MinX + 1; // grid = 0.25
    public int SizeZ => MaxZ - MinZ + 1;
    
    private bool[,] _collision;
    public GameObject?[,] Objects;
    private GameObject?[,] _objectsAir;
    private ushort[,] _objectPlayer;

    private int _regionIdGenerator = 0;
    private List<Region> _regionGraph = new();
    private int[,] _connectionMatrix;

    private int[,] _dist;
    private int[,] _parents;
    
    #region RegionDirection

    private static readonly List<Vector2Int> West = new()
    {
        new Vector2Int(-248, -64),
        new Vector2Int(-76, -64),
        new Vector2Int(-76, 80),
        new Vector2Int(-248, 80)
    };

    private static readonly List<Vector2Int> South = new()
    {
        new Vector2Int(-52, -64),
        new Vector2Int(-52, -84),
        new Vector2Int(52, -84),
        new Vector2Int(52, -64)
    };

    private static readonly List<Vector2Int> East = new()
    {
        new Vector2Int(60, 40),
        new Vector2Int(60, -64),
        new Vector2Int(216, -64),
        new Vector2Int(216, 40),
    };

    private static readonly List<Vector2Int> North = new()
    {
        new Vector2Int(-76, 200),
        new Vector2Int(-76, 80),
        new Vector2Int(60, 80),
        new Vector2Int(60, 200)
    };

    #endregion
    
    public void MapSetting()
    {
        DivideRegionMid();
        DivideRegion(East, 6f);
        DivideRegion(West, 6f);
        DivideRegion(South, 6f);
        DivideRegion(North, 6f);
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

        Pos pos = new Pos();
        while (startZ >= minZ && startX >= minX && startZ < maxZ && startX < maxX)
        {
            for (int i = startZ - size; i <= startZ + size; i++)
            {
                for (int j = startX - size; j <= startX + size; j++)
                {
                    if (i >= 0 && i < maxZ && j >= 0 && j < maxX && _collision[i, j] == false)
                    {
                        pos = new Pos { Z = i, X = j };
                    }
                }
            }

            size++;
            startZ = centerZ - size;
            startX = centerX - size;
        }

        return pos;
    }

    private void DivideRegion(List<Vector2Int> region, float lenSide)
    {
        int len = (int)(lenSide * 4);
	    
        int minX = region.Min(v => v.X);
        int maxX = region.Max(v => v.X);
        int minZ = region.Min(v => v.Z);
        int maxZ = region.Max(v => v.Z);

        int sideX = GetSideLen(maxX - minX, len);
        int sideZ = GetSideLen(maxZ - minZ, len);

        int remainX = (maxX - minX) % sideX;
        int remainZ = (maxZ - minZ) % sideZ;

        for (int i = minZ; i < maxZ - remainZ; i += sideZ)
        {
            for (int j = minX; j < maxX - remainX; j += sideX)
            {
                int lenZ = sideZ;
                int lenX = sideX;
                if (i + 2 * sideZ > maxZ) lenZ += remainZ;
                if (j + 2 * sideX > maxX) lenX += remainX;

                List<Pos> vertices = new List<Pos>
                {
                    Cell2Pos(new Vector2Int(j, i)),
                    Cell2Pos(new Vector2Int(j, i + lenZ)),
                    Cell2Pos(new Vector2Int(j + lenX, i + lenZ)),
                    Cell2Pos(new Vector2Int(j + lenX, i))
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

    private void DivideRegionMid()
    {
        List<Pos> vertices = new List<Pos>
        {
            Cell2Pos(new Vector2Int(-76, 80)),
            Cell2Pos(new Vector2Int(60, 80)),
            Cell2Pos(new Vector2Int(-76, -64)),
            Cell2Pos(new Vector2Int(60, -64))
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
                double meetZ = (v[j].Z - v[i].Z) * (double)(pos.X - v[i].X) / (v[j].X - v[i].X) + v[i].Z;
                if (pos.Z < meetZ) crosses++;
            }
        
            if (crosses % 2 > 0) return region.Id;
        }

        return int.MaxValue;
    }
    
    private int GetSideLen(float len, float minSide)
    {
        return len / 2 < minSide ? (int)len : GetSideLen(len / 2, minSide);
    }
    
    public Pos Cell2Pos(Vector2Int cell)
    {
        // CellPos -> ArrayPos
        return new Pos(MaxZ - cell.Z, cell.X - MinX);
    }

    public Vector2Int Pos2Cell(Pos pos)
    {
        // ArrayPos -> CellPos
        return new Vector2Int(pos.X + MinX, MaxZ - pos.Z);
    }

    public Vector2Int Vector3To2(Vector3 v)
    {
        return new Vector2Int((int)(v.X * 4), (int)(v.Z * 4));
    }
	
    public Vector3 Vector2To3(Vector2Int v, float h = 6f)
    {
        return new Vector3((float)(v.X * 0.25), h, (float)(v.Z * 0.25));
    }

    private static int[] _deltaZ = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static int[] _deltaX = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static int[] _cost = { 10, 14, 10, 14, 10, 14, 10, 14 };

	public List<Vector3> FindPath(GameObject gameObject, Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true)
    {
		// 점수 매기기
		// F = G + H
		// F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
		// G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
		// H = 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정)
        HashSet<Pos> closeList = new HashSet<Pos>();

		// (y, x) 가는 길을 한 번이라도 발견했는지
		// 발견X => MaxValue
		// 발견O => F = G + H
		Dictionary<Pos, int> openList = new Dictionary<Pos, int>();
		Dictionary<Pos, Pos> parent = new Dictionary<Pos, Pos>();
		PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();                                                          // 오픈리스트에 있는 정보들 중에서 가장 좋은 후보를 빠르게 뽑아오기 위한 도구

		Pos pos = Cell2Pos(startCellPos);
		Pos dest = Cell2Pos(destCellPos);
        
		openList.Add(pos, 10 * (Math.Abs(dest.Z - pos.Z) + Math.Abs(dest.X - pos.X)));                                   // 시작점 발견 (예약 진행)
		pq.Push(new PQNode { F = 10 * (Math.Abs(dest.Z - pos.Z) + Math.Abs(dest.X - pos.X)), G = 0, Z = pos.Z, X = pos.X });
		parent.Add(pos, pos);

		while (pq.Count > 0)
		{
			PQNode pqNode = pq.Pop();                                                                                    // 제일 좋은 후보를 찾는다
			Pos node = new Pos(pqNode.Z, pqNode.X);
			if (closeList.Contains(node)) continue;                                                                      // 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
            
			closeList.Add(node);                                                                                         // 제일 좋은 후보를 찾는다
			if (node.Z == dest.Z && node.X == dest.X) break;                                                             // 목적지 도착했으면 바로 종료
            
			for (int i = 0; i < _deltaZ.Length; i++)                                                                     // 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
			{
				Pos next = new Pos(node.Z + _deltaZ[i], node.X + _deltaX[i]);
                
				if (next.Z != dest.Z || next.X != dest.X)                                                                // 유효 범위를 벗어났으면 스킵
				{                                                                                                        // 벽으로 막혀서 갈 수 없으면 스킵
                    if (gameObject.UnitType == 0)
                    {
                        if (CanGo(gameObject, Pos2Cell(next), checkObjects) == false) continue; // CellPos
                    }
                    else
                    {
                        if (CanGoAir(gameObject, Pos2Cell(next), checkObjects) == false) continue;
                    }
				}
                
				if (closeList.Contains(next)) continue;                                                                  // 이미 방문한 곳이면 스킵

				int g = pqNode.G + _cost[i];                                                                          // 비용 계산 : node.G + _cost[i];
                int h = 10 * (Math.Abs(dest.Z - next.Z) + Math.Abs(dest.X - next.X));
				if (openList.TryGetValue(next, out int value) == false) value = Int32.MaxValue;                          // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
				if (value < g + h) continue;
                
				if (openList.TryAdd(next, g + h) == false) openList[next] = g + h;                                       // 예약 진행
				pq.Push(new PQNode { F = (g + h), G = g, Z = next.Z, X = next.X });
				if (parent.TryAdd(next, node) == false) parent[next] = node;
			}
		}

        return gameObject.UnitType == 0 
            ? CalcCellPathFromParent(parent, dest) 
            : CalcCellPathFromParent(parent, dest, 9.0f);
    }
	
    private List<Vector3> CalcCellPathFromParent(Dictionary<Pos, Pos> parent, Pos dest, float height = 6.0f)
    {
        List<Vector3> cells = new();
        
        Pos pos = dest;
        if (!parent.TryGetValue(pos, out var p)) return cells;
        while (parent[pos] != pos)
        {
            cells.Add(Vector2To3(Pos2Cell(pos), height));
            pos = parent[pos];
        }
        cells.Add(Vector2To3(Pos2Cell(pos), height));
        cells.Reverse();

        return cells;
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
                            if (Objects[k, l] != null) cnt++;
                        }
                    }
                    if (cnt == 0) return pos;
                    cnt = 0;
                }
            }
            
            move++;
        } while (true);
    }

    public Vector3 GetClosestPoint(Vector3 cellPos, GameObject target)
    {
        Vector3 targetCellPos = target.CellPos;
        Vector3 destVector;
        
        double sizeX = 0.25 * (target.Stat.SizeX - 1);
        double sizeZ = 0.25 * (target.Stat.SizeZ - 1);
        
        double deltaX = cellPos.X - targetCellPos.X; // P2 cellPos , P1 targetCellPos
        double deltaZ = cellPos.Z - targetCellPos.Z;
        double theta = Math.Round(Math.Atan2(deltaZ, deltaX) * (180 / Math.PI), 2);
        double x;
        double y = target.Stat.UnitType == 0 ? 6 : 8;
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
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z)); // 0.27 이런좌표를 0.25로 변환 
                    break;
                case <= -45 and >= -135:        // down
                    z = targetCellPos.Z - sizeZ;
                    x = (z - zIntercept) / slope;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
                case > -45 and < 45:            // right
                    x = targetCellPos.X + sizeX;
                    z = slope * x - zIntercept;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
                default:                        // left
                    x = targetCellPos.X - sizeX;
                    z = slope * x - zIntercept;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
            }
        }
        else
        {
            switch (theta)
            {
                case >= 45 and <= 135:          // up
                    z = targetCellPos.Z + sizeZ;
                    destVector = Util.Util.NearestCell(new Vector3(target.CellPos.X, (float)y, (float)z)); 
                    break;
                default:
                    z = targetCellPos.Z - sizeZ;
                    destVector = Util.Util.NearestCell(new Vector3(target.CellPos.X, (float)y, (float)z)); 
                    break;
            }
        }

        return destVector;
    }
}