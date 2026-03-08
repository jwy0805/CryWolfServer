using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;

namespace Server.Game;

public struct Pos(int z, int x) : IEquatable<Pos>
{
    public int Z = z;
    public int X = x;
    
    public static bool operator ==(Pos lhs, Pos rhs)
    {
        return lhs.Z == rhs.Z && lhs.X == rhs.X;
    }

    public static bool operator !=(Pos lhs, Pos rhs)
    {
        return !(lhs == rhs);
    }

    public override bool Equals(object? obj) => obj is Pos other && Equals(other);
    public bool Equals(Pos other) => Z == other.Z && X == other.X;
    
    public override int GetHashCode() => HashCode.Combine(Z, X);
    
    public static int DistSqPos(Pos p1, Pos p2)
    {
        int dz = p1.Z - p2.Z;
        int dx = p1.X - p2.X;
        return dz * dz + dx * dx;
    }
}

public struct PQNode : IComparable<PQNode>
{
    public int F;
    public int G;
    public int Z;
    public int X;

    public int Tie;

    public int CompareTo(PQNode other)
    {
        int comparison = F.CompareTo(other.F);
        if (comparison != 0) return comparison;
        
        // 동점: F가 같을 때 Z 방향 경로 우선
        comparison = Tie.CompareTo(other.Tie);
        if (comparison != 0) return comparison;
        
        // 그래도 동점이면 G가 작은 경로 우선
        return G.CompareTo(other.G);
    }
}

public struct Vector2Int(int x, int z)
{
    public int X = x;
    public int Z = z;

    public static Vector2Int Zero => new Vector2Int(0, 0);
    public static Vector2Int One => new Vector2Int(1, 1);
    
    public static Vector2Int operator +(Vector2Int v1, Vector2Int v2)
    {
        return new Vector2Int(v1.X + v2.X, v1.Z + v2.Z);
    }
    
    public static Vector2Int operator -(Vector2Int v1, Vector2Int v2)
    {
        return new Vector2Int(v1.X - v2.X, v1.Z - v2.Z);
    }
}

public struct ClosestVectorInfo
{
    public Vector3 Vector3;
    public float Distance;
}

public struct Region(int id, int[] zCoordinates)
{
    public readonly int Id = id;
    public readonly int[] ZCoordinates = zCoordinates;
}

public partial class Map
{
    private int _minX;
    private int _maxX;
    private int _minZ;
    private int _maxZ;
    private int _sizeX; // grid = 0.25
    private int _sizeZ;
    
    private bool[,] _collision;
    private GameObject?[,] _objects;
    private GameObject?[,] _objectsAir;
    private ushort[,] _objectPlayer;

    private int _regionIdGenerator = 0;
    private List<Region> _regionGraph = new();
    private int[,] _connectionMatrix;

    private int[,] _dist;
    private int[,] _parents;
    
    // Z 우선 탐색
    private int[] _deltaZ = { 1, -1, 0, 0, -1, -1, 1, 1 };
    private int[] _deltaX = { 0, 0, 1, -1, -1, 1, -1, 1 };
    private readonly HashSet<Pos> _pfClosed = new(1024);
    private readonly Dictionary<Pos, int> _pfOpen = new(1024);
    private readonly Dictionary<Pos, Pos> _pfParent = new(1024);
    private readonly PriorityQueue<PQNode> _pfPq = new();
    private readonly Queue<Pos> _bfsQueue = new();
    private int[,] _visited;
    private int _visitedId;
    
    public void MapSetting()
    {
        DivideRegion();
        _connectionMatrix = RegionConnectivity();

        int cnt = _regionGraph.Count;
        _dist = new int[cnt, cnt];
        _parents = new int[cnt, cnt];

        for (int i = 0; i < _regionGraph.Count; i++) Dijkstra(i);
    }
    
    private void DivideRegion()
    {
        int[] yCoordinates = GameData!.ZCoordinatesOfMap;
        for (int i = 0; i < yCoordinates.Length - 1; i++)
        {
            Region region = new(_regionIdGenerator, new [] {yCoordinates[i], yCoordinates[i + 1]});
            _regionGraph.Add(region);
            _regionIdGenerator++;
        }
    }

    private int[,] RegionConnectivity() // -1: not connected, (10, 14): connected, 연결 정보를 확인할 수 있는 2차원 배열 생성
    {
        int count = _regionGraph.Count;
        int[,] connectionMatrix = new int[count, count];

        for (int i = 0; i < count; i++)
        {
            int[] origin = _regionGraph[i].ZCoordinates;
            
            for (int j = 0; j < count; j++)
            {
                if (i == j)
                {
                    connectionMatrix[i, j] = -1;
                    continue;
                }
                
                int[] compared = _regionGraph[j].ZCoordinates;
                connectionMatrix[i, j] = origin.Intersect(compared).Any() ? 10 : -1;
            }
        }

        return connectionMatrix;
    }

    private void Dijkstra(int start)
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
                if (distance[i] == int.MaxValue || distance[i] >= closest) continue;
                
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

    private List<int> RegionPath(int startRegionId, int destRegionId)
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
    
    private int GetRegionByVector(Vector2Int vector)
    {
        int z = vector.Z;
        foreach (var region in _regionGraph)
        {
            int min = region.ZCoordinates[0];
            int max = region.ZCoordinates[1];
            if (min > max) (min, max) = (max, min);
            if (z < max && z >= min) return region.Id;
        }

        return int.MaxValue;
    }
    
    private Vector2Int GetCenter(int regionId, GameObject go, Vector2Int start, Vector2Int dest)
    {
        Region? found = null;
        for (int i = 0; i < _regionGraph.Count; i++)
        {
            if (_regionGraph[i].Id == regionId)
            {
                found = _regionGraph[i];
                break;
            }    
        }

        if (found == null) return new Vector2Int();

        int a = found.Value.ZCoordinates[0];
        int b = found.Value.ZCoordinates[1];
        if (a > b) (a, b) = (b, a);
        
        var center = new Vector2Int(start.X, (a + b) / 2);
        if (CanGo(go, center)) return center;
        
        return go.Target != null ? Vector3To2(GetClosestPoint(go, go.Target)) : center;
    }
    
    public Pos Cell2Pos(Vector2Int cell)
    {   
        // CellPos -> ArrayPos
        return new Pos(_maxZ - cell.Z, cell.X - _minX);
    }

    public Vector2Int Pos2Cell(Pos pos)
    {   
        // ArrayPos -> CellPos
        return new Vector2Int(pos.X + _minX, _maxZ - pos.Z);
    }

    public Vector2Int Vector3To2(Vector3 v)
    {
        return new Vector2Int((int)(v.X * CellCnt), (int)(v.Z * CellCnt));
    }
	
    public Vector3 Vector2To3(Vector2Int v, float h = 6f)
    {
        return new Vector3(v.X * (1 / (float)CellCnt), h, v.Z * (1 / (float)CellCnt));
    }

	public bool TryFindPath(
        GameObject gameObject, 
        Vector2Int startCellPos, 
        Vector2Int destCellPos, 
        List<Vector3> outPath,
        int stopRange = 0, 
        bool stopWhenInRange = true,
        bool checkObjects = true)
    {   
        _pfClosed.Clear();
        _pfOpen.Clear();
        _pfParent.Clear();
        _pfPq.Clear();
		// F = G + H
		// F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
		// G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
		// H = 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정)
        HashSet<Pos> closeList = _pfClosed;

		// (y, x) 가는 길을 한 번이라도 발견했는지
		// 발견X => MaxValue
		// 발견O => F = G + H
        Dictionary<Pos, int> openList = _pfOpen;
        Dictionary<Pos, Pos> parent = _pfParent;
        PriorityQueue<PQNode> pq = _pfPq;                                                        
		Pos start = Cell2Pos(startCellPos);
		Pos dest = Cell2Pos(destCellPos);
        Pos goal = dest;
        if (stopRange < 0 || !stopWhenInRange) stopRange = 0;

        bool reached = false;
        int sizeX = gameObject.SizeX - 1;
        int sizeZ = gameObject.SizeZ - 1;
        int startDx = Math.Abs(dest.X - start.X);
        int startDz = Math.Abs(dest.Z - start.Z);

        if (stopRange > 0)
        {
            startDx = Math.Max(0, startDx - stopRange);
            startDz = Math.Max(0, startDz - stopRange);
        } 
        
        int startH = OctileH(startDx, startDz);

        openList[start] = 0;
        parent[start] = start;
		pq.Push(new PQNode { F = startH, G = 0, Z = start.Z, X = start.X, Tie = ZFirstWhenTie(dest, start) });

        while (pq.Count > 0)
		{
            if (parent.Count > 800)
            {
                // Important Annotation 길찾기 실패 (경로가 존재하지 않음)
                Console.WriteLine($"Pathfinding failed {gameObject.Id} -> start : {Vector2To3(startCellPos)} dest : {Vector2To3(destCellPos)}");
                return false;
            }                                                           
            
            // 제일 좋은 후보를 찾는다
			PQNode pqNode = pq.Pop()!;                                                                                    
			Pos node = new Pos(pqNode.Z, pqNode.X);
            
            // 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
			if (!closeList.Add(node)) continue;    
            
            // 유닛 사거리 고려, Region hop일 땐 정확 도착만 성공
            if ((stopRange > 0 && stopWhenInRange && IsGoal(node, dest, stopRange)) ||
                (stopRange == 0 && node.Z == dest.Z && node.X == dest.X))                                                                                   // 목적지 도착했는지 확인
            {
                goal = node;
                reached = true;
                break;
            }                                                        
            
			for (int i = 0; i < _deltaZ.Length; i++)                                                                     // 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
			{
				Pos next = new Pos(node.Z + _deltaZ[i], node.X + _deltaX[i]);
                
				if (next.Z != dest.Z || next.X != dest.X)                                                               
                {   
                    // GameObject Size 범위에서는 checkObjects를 사용하지 않음, 유효 범위를 벗어났으면 스킵
                    if (Math.Abs(next.Z - start.Z) <= sizeZ && Math.Abs(next.X - start.X) <= sizeX)
                    {
                        if (!CanGo(gameObject, Pos2Cell(next))) continue;
                    }
                    else
                    {
                        // 벽으로 막혀서 갈 수 없으면 스킵
                        if (!CanGo(gameObject, Pos2Cell(next), checkObjects)) continue;
                    }
                }
				if (closeList.Contains(next)) continue;                                                                  // 이미 방문한 곳이면 스킵

                int stepCost = (_deltaZ[i] == 0 || _deltaX[i] == 0) ? 10 : 14;
                int newG = pqNode.G + stepCost;
                int knownBestG = openList.GetValueOrDefault(next, int.MaxValue);
                if (knownBestG <= newG) continue;                                                                        // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
                
                openList[next] = newG;
                parent[next] = node;

                int dx = Math.Abs(dest.X - next.X);
                int dz = Math.Abs(dest.Z - next.Z);
                if (stopRange > 0 && stopWhenInRange)
                {
                    dx = Math.Max(0, dx - stopRange);
                    dz = Math.Max(0, dz - stopRange);
                }
                
                int octileH = OctileH(dx, dz);                                                                                 // 휴리스틱 계산 : 옥타일 거리
                int f = newG + octileH;                                                                                        // 최종 점수 계산 : F = G + H
                
				pq.Push(new PQNode { F = f, G = newG, Z = next.Z, X = next.X, Tie = ZFirstWhenTie(dest, next) });
			}
		}

        if (!reached)
        {
            outPath.Clear();
            return false;
        }

        float h = gameObject.UnitType == 0 ? 6.0f : 8.5f;
        return CalcCellPathFromParent(parent, goal, outPath, h);

        int OctileH(int dx, int dz)
        {
            int dMin = Math.Min(dx, dz);
            int dMax = Math.Max(dx, dz);
            return 10 * (dMax - dMin) + 14 * dMin;
        }
        
        int ZFirstWhenTie(Pos destPos, Pos node)
        {
            int dz = Math.Abs(destPos.Z - node.Z);
            int dx = Math.Abs(destPos.X - node.X);
            // Z 방향으로 우선 움직이도록 하는 가중치
            return dz * 1000 + dx;
        }

        bool IsGoal(Pos node, Pos destNode, int range)
        {
            if (range == 0) return node.Z == destNode.Z && node.X == destNode.X;
            int dz = destNode.Z - node.Z;
            int dx = destNode.X - node.X;
            return dz * dz + dx * dx <= range * range;
        }
    }
    
    private bool CalcCellPathFromParent(
        Dictionary<Pos, Pos> parent, Pos dest, List<Vector3> outPath, float height = 6.0f)
    {
        outPath.Clear();
        if (!parent.ContainsKey(dest)) return false;
        Pos pos = dest;
        while (true)
        {
            outPath.Add(Vector2To3(Pos2Cell(pos), height));
            if (!parent.TryGetValue(pos, out var p))
            {
                outPath.Clear();
                return false;
            }
            
            if (p == pos) break;
            
            pos = p;
        }

        outPath.Reverse();

        return outPath.Count > 0;
    }
    
    public Vector2Int FindNearestEmptySpace(Vector2Int vector, GameObject gameObject)
    {
        return FindNearestEmptySpaceCore(vector, gameObject, null, null);
    }
    
    public Vector2Int FindNearestEmptySpaceMonster(Vector2Int vector, Vector2Int fenceStartCell, GameObject gameObject)
    {
        Pos fencePos = Cell2Pos(fenceStartCell); 
        int halfZ = gameObject.SizeZ - 1;
        int limitZ = fencePos.Z - halfZ - 1;

        return FindNearestEmptySpaceCore(vector, gameObject, limitZ, Constraint);

        bool Constraint(Pos center) => center.Z + halfZ < fencePos.Z;
    }
    
    private Vector2Int FindNearestEmptySpaceCore(
        Vector2Int startCell, GameObject gameObject, int? maxAllowedZ, Func<Pos, bool>? constraint)
    {
        if (GameData == null) return startCell;
        
        Pos start = Cell2Pos(startCell);
        int halfX = gameObject.SizeX - 1;
        int halfZ = gameObject.SizeZ - 1;
        if ((maxAllowedZ == null || start.Z <= maxAllowedZ.Value) && IsValid(start)) return startCell;
        
        _bfsQueue.Clear();
        _bfsQueue.Enqueue(start);
        _visitedId++;
        if (_visitedId == int.MaxValue)
        {
            Array.Clear(_visited, 0, _visited.Length);
            _visitedId = 1;
        }
        
        _visited[start.Z, start.X] = _visitedId;

        ReadOnlySpan<Pos> directions = [new(0, 1), new(0, -1), new(1, 0), new(-1, 0)];

        while (_bfsQueue.Count > 0)
        {
            int levelCount = _bfsQueue.Count;
            Pos bestPos = default;
            int bestDistSq = int.MaxValue;
            bool foundAtThisLevel = false;

            for (int i = 0; i < levelCount; i++)
            {
                Pos current = _bfsQueue.Dequeue();
                
                if ((maxAllowedZ == null || current.Z <= maxAllowedZ.Value) && IsValid(current))
                {
                    foundAtThisLevel = true;
                    
                    // 같은 bfs 레벨 -> game object와 가장 가까운 좌표 선택
                    int distSq = Pos.DistSqPos(start, current);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestPos = current;
                    }
                }

                // 같은 레벨 후보 모두 탐색
                foreach (var direction in directions)
                {
                    Pos next = new Pos(current.Z + direction.Z, current.X + direction.X);
                    if (next.Z < 0 || next.Z >= _sizeZ || next.X < 0 || next.X >= _sizeX) continue;
                    if (_visited[next.Z, next.X] == _visitedId) continue;
                    if (maxAllowedZ != null && next.Z > maxAllowedZ.Value) continue;
                    
                    _visited[next.Z, next.X] = _visitedId;
                    _bfsQueue.Enqueue(next);
                }
            }

            if (foundAtThisLevel) return Pos2Cell(bestPos);
        }

        return startCell;
        
        bool IsValid(Pos p) => (constraint == null || constraint(p)) && IsRectEmpty(p, halfX, halfZ);
    }

    private bool IsRectEmpty(Pos center, int halfX, int halfZ)
    {
        int minX = center.X - halfX;
        int maxX = center.X + halfX;
        int minZ = center.Z - halfZ;
        int maxZ = center.Z + halfZ;
        if (minX < 0 || maxX >= _sizeX || minZ < 0 || maxZ >= _sizeZ) return false;

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (_objects[z, x] != null) return false;
            }
        }

        return true;
    }
    
    public Vector3 GetClosestPoint(GameObject gameObject, GameObject target)
    {
        Vector3 targetCellPos = new Vector3(target.CellPos.X, target.CellPos.Y, target.CellPos.Z);
        Vector3 destVector;
        double sizeX = 0.25 * (target.Stat.SizeX - 1);
        double sizeZ = 0.25 * (target.Stat.SizeZ - 1);
        double deltaX = targetCellPos.X - gameObject.CellPos.X; // P2 cellPos , P1 targetCellPos
        double deltaZ = targetCellPos.Z - gameObject.CellPos.Z;
        double theta = Math.Round(Math.Atan2(deltaZ, deltaX) * (180 / Math.PI), 2);
        double x;
        double y = target.UnitType == 0 ? GameData!.GroundHeight : GameData!.AirHeight;
        double z;

        if (deltaX != 0)
        {
            double slope = deltaZ / deltaX;
            double zIntercept = targetCellPos.Z - slope * targetCellPos.X;

            switch (theta)
            {
                case >= 45 and <= 135:          // up (target is above object)
                    z = targetCellPos.Z - sizeZ;
                    x = (z - zIntercept) / slope;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));            // 0.27 이런좌표를 0.25로 변환 
                    break;
                case <= -45 and >= -135:        // down
                    z = targetCellPos.Z + sizeZ;
                    x = (z - zIntercept) / slope;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
                case > -45 and < 45:            // right
                    x = targetCellPos.X - sizeX;
                    z = slope * x + zIntercept;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
                default:                        // left
                    x = targetCellPos.X + sizeX;
                    z = slope * x + zIntercept;
                    destVector = Util.Util.NearestCell(new Vector3((float)x, (float)y, (float)z));
                    break;
            }
        }
        else
        {
            if (deltaZ > 0)                     // up
            {
                z = targetCellPos.Z - sizeZ;   
            }
            else                                // down
            {
                z = targetCellPos.Z + sizeZ;   
            }

            destVector = Util.Util.NearestCell(
                new Vector3(target.CellPos.X, (float)y, (float)z));
        }

        return destVector;
    }
}