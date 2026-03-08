using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Server.Game;

public partial class GameRoom
{
    private readonly List<GameObject> _candidateBuffer = new(256);
    private float[] _distSqBuffer = new float[256];
    private int[] _orderBuffer = new int[256];

    public bool TryPickTargetAndPath(
        Creature attacker,
        int attackType,
        float attackRange,
        List<Vector3> outPath,
        out GameObject? target,
        bool requiredPath, // Tower는 사거리 내면 바로 공격(false), 이동 유닛은 길찾기 성공해야 공격(true)
        bool checkObjects = true)
    {
        target = null;
        outPath.Clear();
        if (attacker.Room?.Map == null) return false;
        
        int range = checked((int)Math.Round((double)attackRange * Map.CellCnt, MidpointRounding.AwayFromZero));
        if (attacker.Buffs.Contains(BuffId.Aggro))
        {
            var prevTarget = attacker.Target;
            if (prevTarget == null) return false;
            if (!prevTarget.Targetable || prevTarget.Room != attacker.Room) return false;
            if (!requiredPath)
            {
                if (IsInAttackRange(attacker, prevTarget, attackRange))
                {
                    target = prevTarget;
                    return true;
                }
                
                return false;
            }

            if (Map.TryGetPath(attacker, range, prevTarget, outPath, checkObjects))
            {
                target = prevTarget;
                return true;
            }

            return false;
        }
        
        Span<GameObjectType> targetTypes = stackalloc GameObjectType[8];
        int typeCount = BuildTargetTypes(attacker, targetTypes);
        if (typeCount == 0) return false;
        
        _candidateBuffer.Clear();
        
        for (int i = 0; i < typeCount; i++)
        {
            AppendTargets(targetTypes[i], _candidateBuffer);
        }

        int n = _candidateBuffer.Count;
        if (n == 0) return false;
        
        EnsureCapacity(n);
        
        Vector3 attackerPos = attacker.CellPos;
        attackerPos.Y = 0;
        for (int i = 0; i < n; i++)
        {
            GameObject candidate = _candidateBuffer[i];
            if (!candidate.Targetable || candidate.Hp <= 0 || candidate.Id == attacker.Id ||
                (attackType != 2 && candidate.UnitType != attackType))
            {
                _distSqBuffer[i] = float.PositiveInfinity;
                _orderBuffer[i] = 1;
                continue;
            }
            // 이동 유닛만 UnreachableIds 체크
            if (requiredPath && attacker.UnreachableIds.Contains(candidate.Id))
            {
                _distSqBuffer[i] = float.PositiveInfinity;
                _orderBuffer[i] = 1;
                continue;
            }
            
            Vector3 closestPoint = Map.GetClosestPoint(attacker, candidate);
            closestPoint.Y = 0;
            _distSqBuffer[i] = Vector3.DistanceSquared(attackerPos, closestPoint);
            // 마킹 버퍼 초기화 (0 = unvisited, 1 = visited)
            _orderBuffer[i] = 0;
        }

        float rangeSquared = attackRange * attackRange;
        for (int attempt = 0; attempt < n; attempt++)
        {
            int bestIdx = -1;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                if (_orderBuffer[i] != 0) continue;
                float dist = _distSqBuffer[i];
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0) break;

            _orderBuffer[bestIdx] = 1;
            GameObject candidate = _candidateBuffer[bestIdx];
            // Tower: 사거리 내면 즉시 선택
            if (!requiredPath)
            {
                if (bestDist <= rangeSquared)
                {
                    target = candidate;
                    return true;
                }

                break;
            }
            
            if (Map.TryGetPath(attacker, range, candidate, outPath, checkObjects))
            {
                target = candidate;
                return true;
            }
            
            attacker.UnreachableIds.Add(candidate.Id);
        }

        return false;
    }

    public bool TryPickPriorityTargetAndPath(
        Creature attacker,
        IReadOnlyList<GameObjectType> priorityTypes,
        int attackType,
        float attackRange,
        List<Vector3> outPath,
        out GameObject? target,
        bool requiredPath = true,
        bool checkObjects = true)
    {
        target = null;
        outPath.Clear();
        if (attacker.Room?.Map == null) return false;
        
        int range = checked((int)Math.Round((double)attackRange * Map.CellCnt, MidpointRounding.AwayFromZero));
        if (attacker.Buffs.Contains(BuffId.Aggro))
        {
            var prevTarget = attacker.Target;
            if (prevTarget == null) return false;
            if (!prevTarget.Targetable || prevTarget.Room != attacker.Room) return false;
            if (!requiredPath)
            {
                if (IsInAttackRange(attacker, prevTarget, attackRange))
                {
                    target = prevTarget;
                    return true;
                }
                
                return false;
            }

            if (Map.TryGetPath(attacker, range, prevTarget, outPath, checkObjects))
            {
                target = prevTarget;
                return true;
            }

            return false;
        }
        
        _candidateBuffer.Clear();

        for (int i = 0; i < priorityTypes.Count; i++)
        {
            _candidateBuffer.Clear();
            AppendTargets(priorityTypes[i], _candidateBuffer);
            
            int n = _candidateBuffer.Count;
            if (n == 0) continue;
            
            EnsureCapacity(n);

            Vector3 attackerPos = attacker.CellPos;
            attackerPos.Y = 0;
            for (int j = 0; j < n; j++)
            {
                GameObject candidate = _candidateBuffer[j];
                if (!candidate.Targetable || candidate.Hp <= 0 || candidate.Id == attacker.Id ||
                    (attackType != 2 && candidate.UnitType != attackType))
                {
                    _distSqBuffer[j] = float.PositiveInfinity;
                    _orderBuffer[j] = 1;
                    continue;
                }
                // 이동 유닛만 UnreachableIds 체크
                if (requiredPath && attacker.UnreachableIds.Contains(candidate.Id))
                {
                    _distSqBuffer[j] = float.PositiveInfinity;
                    _orderBuffer[j] = 1;
                    continue;
                }
            
                Vector3 closestPoint = Map.GetClosestPoint(attacker, candidate);
                closestPoint.Y = 0;
                _distSqBuffer[j] = Vector3.DistanceSquared(attackerPos, closestPoint);
                // 마킹 버퍼 초기화 (0 = unvisited, 1 = visited)
                _orderBuffer[j] = 0;
            }
            
            float rangeSquared = attackRange * attackRange;
            for (int attempt = 0; attempt < n; attempt++)
            {
                int bestIdx = -1;
                float bestDist = float.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    if (_orderBuffer[j] != 0) continue;
                    float dist = _distSqBuffer[j];
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = j;
                    }
                }

                if (bestIdx < 0) break;

                _orderBuffer[bestIdx] = 1;
                GameObject candidate = _candidateBuffer[bestIdx];
                // Tower: 사거리 내면 즉시 선택
                if (!requiredPath)
                {
                    if (bestDist <= rangeSquared)
                    {
                        target = candidate;
                        return true;
                    }

                    break;
                }
                
                if (Map.TryGetPath(attacker, range, candidate, outPath, checkObjects))
                {
                    target = candidate;
                    return true;
                }
                
                attacker.UnreachableIds.Add(candidate.Id);
            }
        }

        return false;
    }

    private bool IsInAttackRange(Creature attacker, GameObject target, float attackRangeWorld)
    {
        Vector3 closestPoint = Map.GetClosestPoint(attacker, target);
        Vector3 attackerPos = attacker.CellPos;
        closestPoint.Y = 0;
        attackerPos.Y = 0;
        
        return Vector3.DistanceSquared(closestPoint, attackerPos) <= attackRangeWorld * attackRangeWorld;
    }

    private void EnsureCapacity(int n)
    {
        // 거리/마킹 버퍼 크기 보장
        if (_distSqBuffer.Length < n) _distSqBuffer = new float[Math.Max(n, _distSqBuffer.Length * 2)];
        if (_orderBuffer.Length < n) _orderBuffer = new int[Math.Max(n, _orderBuffer.Length * 2)];
    }
    
    private int BuildTargetTypes(Creature attacker, Span<GameObjectType> targetTypes)
    {
        int count = 0;
        bool reachable = ReachableInFence(attacker);
        if (attacker.ObjectType == GameObjectType.Monster)
        {
            targetTypes[count++] = GameObjectType.Tower;
            if (!reachable) targetTypes[count++] = GameObjectType.Fence;
            if (reachable) targetTypes[count++] = GameObjectType.Sheep;
        }
        else
        {
            targetTypes[count++] = GameObjectType.Monster;
            targetTypes[count++] = GameObjectType.MonsterStatue;
            targetTypes[count++] = GameObjectType.Portal;
        }

        return count;
    }

    private bool ReachableInFence(GameObject gameObject)
    {
        var type = gameObject.ObjectType;
        if (type is GameObjectType.Tower or GameObjectType.Sheep) return true;
        if (type is GameObjectType.Monster && gameObject.Stat.UnitType == 1) return true;
        return GameInfo.NorthFenceCnt < GameInfo.NorthMaxFenceCnt;
    }
    
    private void AppendTargets(GameObjectType type, List<GameObject> buffer)
    {
        switch (type)
        {
            case GameObjectType.Monster:
                foreach (var m in _monsters.Values) buffer.Add(m);
                break;
            case GameObjectType.MonsterStatue:
                foreach (var s in _statues.Values) buffer.Add(s);
                break;
            case GameObjectType.Tower:
                foreach (var t in _towers.Values) buffer.Add(t);
                break;
            case GameObjectType.Sheep:
                foreach (var s in _sheeps.Values) buffer.Add(s);
                break;
            case GameObjectType.Fence:
                foreach (var f in _fences.Values) buffer.Add(f);
                break;
            case GameObjectType.Portal:
                if (_portal != null) buffer.Add(_portal);
                break;
        }
    }

    public List<GameObject> FindTargetsInRectangle(GameObject gameObject, IEnumerable<GameObjectType> typeList,
        float width, float height, float degree, int attackType = 0)
    {
        // 1. 타겟 리스트 생성
        var targetList = new List<GameObject>();
        foreach (var type in typeList) AppendTargets(type, targetList);
        if (targetList.Count == 0) return targetList;
        
        // 2. 현재 위치 계산
        Vector3 center = gameObject.CellPos;
        
        // 3. 필터링할 목표물 리스트 초기화
        var objectsInRectangle = new List<GameObject>();
        
        // 4. 직사각형 모서리 좌표 계산
        float halfWidth = width / 2;
        float halfHeight = height / 2;
        List<Vector3> corners = new List<Vector3>
        {
            center with { X = center.X - halfWidth },
            new(center.X - halfWidth, center.Y, center.Z + halfHeight * 2),
            new(center.X + halfWidth, center.Y, center.Z + halfHeight * 2),
            center with { X = center.X + halfWidth }
        };
        
        // 5. 직사각형 모서리 회전
        List<Vector3> rotatedCorners = corners.Select(corner => 
            corner.RotateAroundPoint(center, degree)).ToList();
        
        // 6. 직사각형 경계 계산
        float minX = rotatedCorners.Min(corner => corner.X);
        float maxX = rotatedCorners.Max(corner => corner.X);
        float minZ = rotatedCorners.Min(corner => corner.Z);
        float maxZ = rotatedCorners.Max(corner => corner.Z);
        
        // 7. 각 목표물에 대해 조건 검사
        foreach (var obj in targetList)
        {
            if (!obj.Targetable || (attackType != 2 && obj.UnitType != attackType)) continue;

            Vector3 targetPos = obj.CellPos;
            if (targetPos.X >= minX && targetPos.X <= maxX && targetPos.Z >= minZ && targetPos.Z <= maxZ)
            {
                objectsInRectangle.Add(obj);
            }
        }

        return objectsInRectangle;
    }
    
    public GameObject? FindMostDenseTargets(List<GameObjectType> searchType, List<GameObjectType> targetType,
        GameObject gameObject, float range, float impactRange, int attackType = 0)
    {
        var targets = FindTargets(gameObject, searchType, range, attackType);
        if (targets.Count == 0) return null;

        GameObject? target = null;
        var maxDensity = 0;
        foreach (var t in targets)
        {
            var aroundTargets = FindTargets(t, targetType, impactRange, attackType);
            if (aroundTargets.Count <= maxDensity) continue;
            maxDensity = aroundTargets.Count;
            target = t;
        }

        return target;
    }

    public GameObject? FindNearestSheep(GameObject gameObject)
    {
        PriorityQueue<TargetDistance, float> pq = new();

        foreach (var sheep in _sheeps.Values)
        {
            var sheepPos = Map.Vector2To3(Map.FindNearestEmptySpace(
                Map.Vector3To2(Map.GetClosestPoint(gameObject, sheep)), gameObject));
            sheepPos = sheepPos with { Y = 0 };
            var cellPos = gameObject.CellPos with { Y = 0 };
            var distance = Vector3.Distance(sheepPos, cellPos);
            pq.Enqueue(new TargetDistance { Target = sheep, Distance = distance }, distance);
        }

        return pq.Count == 0 ? null : pq.Dequeue().Target;
    }
    
    public GameObject? FindNearestTower(List<UnitId> unitIdList)
    {
        Dictionary<int, GameObject> targetDict = new();
        foreach (var unitId in unitIdList)
        {
            foreach (var (key, tower) in _towers)
            {
                if (tower.UnitId == unitId) targetDict.Add(key, tower);
            }
        }

        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        foreach (var go in targetDict.Values)
        {
            Vector3 targetPos = go.CellPos;
            bool targetable = go.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - go.CellPos);
            if (dist < closestDist && targetable)
            {
                closestDist = dist;
                target = go;
            }
        }

        return target;
    }

    /// <summary>
    /// Find multiple targets in the range of dist from GameObject
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="typeList"></param>
    /// <param name="dist"></param>
    /// <param name="attackType"></param>
    /// <returns>"plural targets in the range."</returns>
    public List<GameObject> FindTargets(
        GameObject gameObject, IEnumerable<GameObjectType> typeList, float dist = 100, int attackType = 0)
    {
        var targetList = new List<GameObject>();
        foreach (var type in typeList) AppendTargets(type, targetList);
        if (targetList.Count == 0) return targetList;
        
        var objectsInDist = targetList
            .Where(obj => obj.Targetable && (attackType == 2 || obj.UnitType == attackType))
            .Where(obj => Vector3.Distance(
                obj.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 }) < dist)
            .ToList();

        return objectsInDist;
    }

    /// <summary>
    /// Find multiple targets in the range of dist from specific CellPos
    /// </summary>
    /// <param name="cellPos"></param>
    /// <param name="typeList"></param>
    /// <param name="dist"></param>
    /// <param name="attackType"></param>
    /// <returns>"plural targets in the range."</returns>
    public List<GameObject> FindTargets(
        Vector3 cellPos, IEnumerable<GameObjectType> typeList, float dist = 100, int attackType = 0)
    {
        var targetList = new List<GameObject>();
        foreach (var type in typeList) AppendTargets(type, targetList);
        if (targetList.Count == 0) return targetList;
        
        var objectsInDist = targetList
            .Where(obj => obj.Stat.Targetable && (attackType == 2 || obj.UnitType == attackType))
            .Where(obj => Vector3.Distance(obj.CellPos with { Y = 0 }, cellPos with { Y = 0 }) < dist)
            .ToList();

        return objectsInDist;
    }
    
    public List<GameObject> FindTargetsInAngleRange(GameObject gameObject, float dir,
        IEnumerable<GameObjectType> typeList, float skillDist = 100, float angle = 30, int attackType = 0)
    {   
        // 1. 타겟 리스트 생성
        var targetList = new List<GameObject>();
        foreach (var type in typeList) AppendTargets(type, targetList);
        if (targetList.Count == 0) return targetList;
        
        // 2. 현재 위치와 전방 벡터 계산
        double radians = dir * (Math.PI / 180);
        Vector3 forward = new Vector3((int)Math.Round(Math.Sin(radians)), 0, (int)Math.Round(Math.Cos(radians)));
      
        // 3. 필터링할 목표물 리스트 초기화
        var objectsInAngleRange = new List<GameObject>();

        // 4. 각 목표물에 대해 조건 검사
        foreach (var obj in targetList)
        {   
            // 4.1 타겟팅 가능한지와 공격 타입이 맞는지 확인
            if (obj.Targetable == false || (attackType != 2 && obj.UnitType != attackType)) continue;
           
            // 4.2 거리와 각도를 계산
            Vector3 dirVector = obj.CellPos - gameObject.CellPos;
            float distance = Vector3.Distance(obj.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 });
            double angleToTarget =
                (Math.Atan2(forward.Z, forward.X) - Math.Atan2(dirVector.Z, dirVector.X))
                * 180 / Math.PI;
            if (angleToTarget > 180) angleToTarget -= 360;
            
            // 4.3 조건에 맞는지 확인
            if (distance < skillDist && Math.Abs(angleToTarget) <= angle / 2)
            {   
                // 조건에 맞으면 리스트에 추가
                objectsInAngleRange.Add(obj);
            }
        }

        return objectsInAngleRange;
    }
    
    /// <summary>
    /// Find multiple targets with specific unitIds or species in the range of dist from GameObject
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="type"></param>
    /// <param name="unitIds"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    public List<GameObject> FindTargetsBySpecies(GameObject gameObject, 
        GameObjectType type, IEnumerable<UnitId> unitIds, float dist = 100)
    {
        var targetList = new List<GameObject>();
        AppendTargets(type, targetList);
        if (targetList.Count == 0) return targetList;

        var objectsInDist = new List<GameObject>();
        switch (type)
        {
            case GameObjectType.Monster:
                objectsInDist = targetList.OfType<Monster>()
                    .Where(obj => unitIds.Contains(obj.UnitId))
                    .Where(obj => Vector3.Distance(
                        obj.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 }) <= dist)
                    .Cast<GameObject>()
                    .ToList();
                break;
            case GameObjectType.Tower:
                objectsInDist = targetList.OfType<Tower>()
                    .Where(obj => unitIds.Contains(obj.UnitId))
                    .Where(obj => Vector3.Distance(
                        obj.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 }) <= dist)
                    .Cast<GameObject>()
                    .ToList();
                break;
        }

        return objectsInDist;
    }

    private PositionInfo FindMonsterSpawnPos(MonsterStatue statue)
    {
        var posInfo = new PositionInfo()
        {
            PosX = statue.PosInfo.PosX,
            PosY = statue.PosInfo.PosY,
            PosZ = statue.PosInfo.PosZ,
            State = State.Idle,
        };
        
        if (statue.Way == SpawnWay.North)
        {
            posInfo.PosZ = statue.PosInfo.PosZ - statue.Stat.SizeZ * 2f;
        }
        else
        {
            posInfo.PosZ = statue.PosInfo.PosZ + statue.Stat.SizeZ * 2f;
        }

        var pos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
        var vec2Pos = Map.Vector3To2(pos);
        var vec2FencePos = Map.Vector3To2(GameInfo.FenceStartPos);
        pos = Map.Vector2To3(Map.FindNearestEmptySpaceMonster(vec2Pos, vec2FencePos, statue));
        posInfo.PosX = pos.X;
        posInfo.PosY = pos.Y;
        posInfo.PosZ = pos.Z;
        
        return posInfo;
    }
    
    public GameObject? FindGameObjectById(int id)
    {
        GameObject? go = new GameObject();
        GameObjectType type = ObjectManager.GetObjectTypeById(id);
        switch (type)
        {
            case GameObjectType.Player:
                if (_players.TryGetValue(id, out var player)) go = player;
                break;
            case GameObjectType.Tower:
                if (_towers.TryGetValue(id, out var tower)) go = tower;
                break;
            case GameObjectType.Sheep:
                if (_sheeps.TryGetValue(id, out var sheep)) go = sheep;
                break;
            case GameObjectType.Monster:
                if (_monsters.TryGetValue(id, out var monster)) go = monster;
                break;
            case GameObjectType.MonsterStatue:
                if (_statues.TryGetValue(id, out var statue)) go = statue;
                break;
            case GameObjectType.Projectile:
                if (_projectiles.TryGetValue(id, out var projectile)) go = projectile;
                break;
            case GameObjectType.Effect:
                if (_effects.TryGetValue(id, out var effect)) go = effect;
                break;
            case GameObjectType.Fence:
                if (_fences.TryGetValue(id, out var fence)) go = fence;
                break;
            default:
                go = null;
                break;
        }

        return go;
    }

    public Vector3[] GetSheepBounds()
    {
        return GameInfo.SheepBounds;
    }
}