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

public struct Region
{
    public readonly int Id;
    public readonly int[] ZCoordinates;

    public Region(int id, int[] zCoordinates)
    {
        Id = id;
        ZCoordinates = zCoordinates;
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
    private GameObject?[,] _objects;
    private GameObject?[,] _objectsAir;
    private ushort[,] _objectPlayer;

    private int _regionIdGenerator = 0;
    private List<Region> _regionGraph = new();
    private int[,] _connectionMatrix;

    private int[,] _dist;
    private int[,] _parents;
    
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
        int[,] connectionMatrix = new int[_regionGraph.Count, _regionGraph.Count];

        for (int i = 0; i < _regionGraph.Count; i++)
        {
            int[] origin = _regionGraph[i].ZCoordinates;
            
            for (int j = 0; j < _regionGraph.Count; j++)
            {
                int[] compared = _regionGraph[j].ZCoordinates;
                
                if (i == j)
                {
                    connectionMatrix[i, j] = -1;
                }
                else
                {
                    if (origin.Intersect(compared).Any())
                    {
                        connectionMatrix[i, j] = 10;
                    }
                    else
                    {
                        connectionMatrix[i, j] = -1;
                    }
                }
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
        foreach (var region in _regionGraph)
        {
            if (vector.Z < region.ZCoordinates.Max() && vector.Z >= region.ZCoordinates.Min()) return region.Id;
        }

        return int.MaxValue;
    }
    
    private Vector2Int GetCenter(int regionId, GameObject go, Vector2Int start, Vector2Int dest)
    {   
        var region = _regionGraph.FirstOrDefault(region => region.Id == regionId);
        if (region.ZCoordinates == null) return new Vector2Int();
        
        double n1 = region.ZCoordinates.Min();
        double n2 = region.ZCoordinates.Max();
        var center = new Vector2Int(start.X, (int)((n1 + n2) / 2));
        
        if (CanGo(go, center)) return center;
        var centerAdjusted = go.Target != null 
            ? Vector3To2(GetClosestPoint(go, go.Target)) : center;
        
        return centerAdjusted;
    }
    
    public Pos Cell2Pos(Vector2Int cell)
    {   // CellPos -> ArrayPos
        return new Pos(MaxZ - cell.Z, cell.X - MinX);
    }

    public Vector2Int Pos2Cell(Pos pos)
    {   // ArrayPos -> CellPos
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

	public List<Vector3> FindPath(
        GameObject gameObject, Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true)
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
		PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();                                                         

		Pos pos = Cell2Pos(startCellPos);
		Pos dest = Cell2Pos(destCellPos);
        
        int sizeX = gameObject.SizeX - 1;
        int sizeZ = gameObject.SizeZ - 1;
        
		openList.Add(pos, 10 * (Math.Abs(dest.Z - pos.Z) + Math.Abs(dest.X - pos.X)));                                  
		pq.Push(new PQNode { F = 10 * (Math.Abs(dest.Z - pos.Z) + Math.Abs(dest.X - pos.X)), G = 0, Z = pos.Z, X = pos.X });
		parent.Add(pos, pos);

        while (pq.Count > 0)
		{
            if (parent.Count > 800)
            {
                // 길찾기 실패 (경로가 존재하지 않음)
                Console.WriteLine($"Pathfinding failed -> start : {Vector2To3(startCellPos)} dest : {Vector2To3(destCellPos)}");
                return new List<Vector3>();
            }                                                           
            
            // 제일 좋은 후보를 찾는다
			PQNode pqNode = pq.Pop();                                                                                    
			Pos node = new Pos(pqNode.Z, pqNode.X);
            
            // 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
			if (closeList.Add(node) == false) continue;    
            
            // 목적지 도착했으면 바로 종료
            if (node.Z == dest.Z && node.X == dest.X) break;                                                            
            
			for (int i = 0; i < _deltaZ.Length; i++)                                                                     // 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
			{
				Pos next = new Pos(node.Z + _deltaZ[i], node.X + _deltaX[i]);
                
				if (next.Z != dest.Z || next.X != dest.X)                                                               
                {   
                    // GameObject Size 범위에서는 checkObjects를 사용하지 않음, 유효 범위를 벗어났으면 스킵
                    if (Math.Abs(next.Z - pos.Z) <= sizeZ && Math.Abs(next.X - pos.X) <= sizeX)
                    {
                        if (CanGo(gameObject, Pos2Cell(next)) == false) { continue; }
                    }
                    else
                    {
                        // 벽으로 막혀서 갈 수 없으면 스킵
                        if (CanGo(gameObject, Pos2Cell(next), checkObjects) == false) { continue; } 
                    }
                }
				if (closeList.Contains(next)) continue;                                                                  // 이미 방문한 곳이면 스킵

				int g = pqNode.G + _cost[i];                                                                             // 비용 계산 : node.G + _cost[i];
                int h = 10 * (Math.Abs(dest.Z - next.Z) + Math.Abs(dest.X - next.X));
				int value = openList.GetValueOrDefault(next, int.MaxValue);                                              // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
                if (value < g + h) continue;
                
				if (openList.TryAdd(next, g + h) == false) openList[next] = g + h;                                       // 예약 진행
				pq.Push(new PQNode { F = (g + h), G = g, Z = next.Z, X = next.X });
				if (parent.TryAdd(next, node) == false) parent[next] = node;
			}
		}

        return gameObject.UnitType == 0 
            ? CalcCellPathFromParent(parent, dest) : CalcCellPathFromParent(parent, dest, 9.0f);
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
    
    public Vector2Int FindNearestEmptySpace(Vector2Int vector, GameObject gameObject)
    {
        Pos pos = Cell2Pos(vector);
        int cnt = 0;
        int move = 0;
        int sizeX = gameObject.SizeX - 1;
        int sizeZ = gameObject.SizeZ - 1;

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
                            if (_objects[k, l] != null) cnt++;
                        }
                    }
                    if (cnt == 0) return Pos2Cell(pos);
                    cnt = 0;
                }
            }
            
            move++;
        } while (true);
    }
    
    public Vector2Int FindNearestEmptySpaceMonster(Vector2Int vector, Vector2Int fenceStartPos, GameObject gameObject)
    {
        Pos pos = Cell2Pos(vector);
        Pos fencePos = Cell2Pos(fenceStartPos);
        int cnt = 0;
        int move = 0;
        int sizeX = gameObject.SizeX - 1;
        int sizeZ = gameObject.SizeZ - 1;

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
                        if (k > fencePos.Z) cnt++;
                        for (int l = pos.X - sizeX; l <= pos.X + sizeX; l++)
                        {
                            if (_objects[k, l] != null) cnt++;
                        }
                    }
                    if (cnt == 0) return Pos2Cell(pos);
                    cnt = 0;
                }
            }
            
            move++;
        } while (true);
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
        
    private PriorityQueue<ClosestVectorInfo, float> GetVectors(GameObject gameObject, GameObject target)
    {
        PriorityQueue<ClosestVectorInfo, float> distances = new();
        Vector3 startPos = gameObject.CellPos with { Y = 0 };
        Vector3 targetPos = target.CellPos with { Y = 0 };
        int sizeX = target.SizeX - 1;
        int sizeZ = target.SizeZ - 1;
        int collisionLengthX = sizeX * 2 + 1;
        int collisionLengthZ = sizeZ * 2 + 1;

        // Traverse the upper side of the target collision box
        for (int i = 0; i < collisionLengthX; i++)
        {
            Vector3 targetVector = targetPos + new Vector3((i - sizeX) * 0.25f, 0, sizeZ * 0.25f);
            float distance = Vector3.Distance(startPos, targetVector);
            ClosestVectorInfo info = new() { Vector3 = targetVector, Distance = distance };
            distances.Enqueue(info, distance);
        }
        
        // Right side
        for (int i = 1; i < collisionLengthZ - 1; i++)
        {
            Vector3 targetVector = targetPos + new Vector3(sizeX * 0.25f, 0, (i - sizeZ) * 0.25f);
            float distance = Vector3.Distance(startPos, targetVector);
            ClosestVectorInfo info = new() { Vector3 = targetVector, Distance = distance };
            distances.Enqueue(info, distance);
        }
        
        // Lower side
        for (int i = collisionLengthX - 1; i >= 0; i--)
        {
            Vector3 targetVector = targetPos + new Vector3((i - sizeX) * 0.25f, 0, sizeZ * -0.25f);
            float distance = Vector3.Distance(startPos, targetVector);
            ClosestVectorInfo info = new() { Vector3 = targetVector, Distance = distance };
            distances.Enqueue(info, distance);
        }
        
        // Left side
        for (int i = collisionLengthZ - 2; i > 0; i--)
        {
            Vector3 targetVector = targetPos + new Vector3(sizeX * -0.25f, 0, (i - sizeZ) * 0.25f);
            float distance = Vector3.Distance(startPos, targetVector);
            ClosestVectorInfo info = new() { Vector3 = targetVector, Distance = distance };
            distances.Enqueue(info, distance);
        }

        return distances;
    }
}