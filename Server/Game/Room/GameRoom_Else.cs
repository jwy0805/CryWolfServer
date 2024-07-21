using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Server.Game;

public partial class GameRoom
{
    private void BaseInit()
    {
        // Spawn Portal
        Portal northPortal = ObjectManager.Instance.Add<Portal>();
        northPortal.Init();
        northPortal.Way = SpawnWay.North;
        northPortal.Info.Name = "Portal#6Red";
        northPortal.CellPos = GameData.PortalPos[0];
        northPortal.Dir = 90;
        Push(EnterGame, northPortal);
    }
    
    public void InfoInit()
    {
        GameInfo = new GameInfo(_players, MapId);
        StorageLevel = 1;
        foreach (var player in _players.Values)
        {
            if (player.Session == null) return;
            
            EnterSheepByServer(player);

            if (player.Camp == Camp.Sheep)
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.SheepResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.MaxSheep, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.SheepCount, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthTower, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthTower, Max = false });
            }
            else
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.WolfResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.MaxSheep, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.SheepCount, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMonster, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMonster, Max = false });
            }
        }
    }

    #region Summary
    /// <summary>
    /// Find a Nearest Target.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="attackType"></param>
    /// <returns>a Nearest Target GameObject</returns>
    #endregion
    public GameObject? FindClosestTarget(GameObject gameObject, int attackType = 0)
    {   // 어그로 끌린 상태면 리턴
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        var targetTypeList = GetTargetType(gameObject, ReachableInFence(gameObject));
        var targetList = new List<GameObject>();
        foreach (var type in targetTypeList) targetList.AddRange(GetTargets(type));

        return MeasureShortestDist(gameObject, targetList, attackType);
    }
    
    public GameObject? FindClosestTarget(GameObject gameObject, List<GameObjectType> typeList, int attackType = 0)
    {   // 어그로 끌린 상태면 리턴
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        var targetList = new List<GameObject>();
        foreach (var type in typeList) targetList.AddRange(GetTargets(type));

        return MeasureShortestDist(gameObject, targetList, attackType);
    }

    public GameObject? FindRandomTarget(GameObject gameObject, List<GameObjectType> typeList, float dist, int attackType = 0)
    {
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        var targetList = new List<GameObject>();
        foreach (var type in typeList) targetList.AddRange(GetTargets(type));

        return GetRandomTarget(gameObject, targetList, dist, attackType);
    }
    
    public GameObject? FindRandomTarget(GameObject gameObject, float dist, int attackType = 0, bool? reachableInFence = null)
    {   
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        var targetTypeList = GetTargetType(gameObject, reachableInFence);
        var targetList = new List<GameObject>();
        foreach (var type in targetTypeList) targetList.AddRange(GetTargets(type));

        return GetRandomTarget(gameObject, targetList, dist, attackType);
    }
    
    private List<GameObjectType> GetTargetType(GameObject gameObject, bool? reachableInFence = null)
    {
        List<GameObjectType> targetTypeList = new();
        reachableInFence ??= ReachableInFence(gameObject);
        switch (gameObject.ObjectType)
        {
            case GameObjectType.Monster:
                if ((bool)reachableInFence)
                {
                    targetTypeList = new List<GameObjectType> 
                        { GameObjectType.Tower, GameObjectType.Sheep };
                }
                else
                {
                    targetTypeList = new List<GameObjectType>
                        { GameObjectType.Tower, GameObjectType.Sheep, GameObjectType.Fence };
                }
                break;
            case GameObjectType.Tower:
                targetTypeList = new List<GameObjectType> { GameObjectType.Monster };
                break;
        }
        
        return targetTypeList;
    }

    private bool ReachableInFence(GameObject go)
    {
        var type = go.ObjectType;
        if (type is GameObjectType.Tower or GameObjectType.Sheep) return true;
        
        if (go.Way == SpawnWay.North)
        {
            if (GameInfo.NorthFenceCnt < GameInfo.NorthMaxFenceCnt) return true;
        }
        else
        {
            if (GameInfo.SouthFenceCnt < GameInfo.SouthMaxFenceCnt) return true;
        }

        return false;
    }
    
    private IEnumerable<GameObject> GetTargets(GameObjectType type)
    {
        List<GameObject> targets = new();
        switch (type)
        {
            case GameObjectType.Monster:
                targets = _monsters.Values.Cast<GameObject>().ToList();
                break;
            case GameObjectType.MonsterStatue:
                targets = _statues.Values.Cast<GameObject>().ToList();
                break;
            case GameObjectType.Tower:
                targets = _towers.Values.Cast<GameObject>().ToList();
                break;
            case GameObjectType.Sheep:
                targets = _sheeps.Values.Cast<GameObject>().ToList();
                break;
            case GameObjectType.Fence:
                targets = _fences.Values.Cast<GameObject>().ToList();
                break;
        }

        return targets;    
    }
    
    private GameObject? MeasureShortestDist(GameObject gameObject, List<GameObject> targets, int attackType)
    {
        GameObject? closest = null;
        var closestDistSq = float.MaxValue;

        foreach (var target in targets)
        {
            if (target.Targetable == false || target.Id == gameObject.Id
                || (target.UnitType != attackType && attackType != 2)) continue;
            var pos = target.PosInfo;
            var targetPos = new Vector3(pos.PosX, 0, pos.PosZ);
            var cellPos = gameObject.CellPos with { Y = 0 };
            var distance = Vector3.Distance(targetPos, cellPos);
            if (distance < closestDistSq == false) continue;
            closest = target;
            closestDistSq = distance;
        }

        return closest;
    }
    
    private GameObject? GetRandomTarget(GameObject gameObject, List<GameObject> targets, float dist, int attackType)
    {
        List<GameObject> targetList = new();
        foreach (var target in targets)
        {
            if (target.Targetable == false || (target.UnitType != attackType && attackType != 2)
                || target.Id == gameObject.Id && attackType != 2) continue;
            var pos = target.PosInfo;
            var targetPos = new Vector3(pos.PosX, 0, pos.PosZ);
            var cellPos = gameObject.CellPos with { Y = 0 };
            var distance = Vector3.Distance(targetPos, cellPos);
            if (distance < dist) targetList.Add(target);
        }

        return targetList.Count == 0 ? null : targetList[new Random().Next(targetList.Count)];
    }

    public List<GameObject> FindTargetsInRectangle(GameObject gameObject, IEnumerable<GameObjectType> typeList,
        float width, float height, float degree, int attackType = 0)
    {
        // 1. 타겟 리스트 생성
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
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
            if (obj.Targetable == false || (attackType != 2 && obj.UnitType != attackType)) continue;

            Vector3 targetPos = obj.CellPos;
            if (targetPos.X >= minX && targetPos.X <= maxX && targetPos.Z >= minZ && targetPos.Z <= maxZ)
            {
                objectsInRectangle.Add(obj);
            }
        }

        return objectsInRectangle;
    }
    
    // public List<GameObject> FindTargetsInRectangle(IEnumerable<GameObjectType> typeList,
    //     GameObject gameObject, double width, double height, int attackType = 0)
    // {
    //     Map map = Map;
    //     Pos pos = map.Cell2Pos(map.Vector3To2(gameObject.CellPos));
    //     
    //     double halfWidth = width / 2.0f;
    //     double angle = gameObject.Dir * Math.PI / 180;
    //
    //     double x1 = pos.X - halfWidth;
    //     double x2 = pos.X + halfWidth;
    //     double z1 = pos.Z - height;
    //     double z2 = pos.Z;
    //     Vector2[] corners = new Vector2[4];
    //     corners[0] = new Vector2((float)x1, (float)z1);
    //     corners[1] = new Vector2((float)x1, (float)z2);
    //     corners[2] = new Vector2((float)x2, (float)z2);
    //     corners[3] = new Vector2((float)x2, (float)z1);
    //     
    //     List<GameObject> gameObjects = FindTargets(gameObject, typeList, (float)(height > width ? height : width));
    //     Vector2[] cornersRotated = RotateRectangle(corners, new Vector2(pos.X, pos.Z), angle);
    //
    //     return (from obj in gameObjects 
    //         where obj.Targetable && attackType == 2 || obj.UnitType == attackType
    //         let objPos = map.Cell2Pos(map.Vector3To2(obj.CellPos)) 
    //         let point = new Vector2(objPos.X, objPos.Z) 
    //         where CheckPointInRectangle(cornersRotated, point, width * height) select obj).ToList();
    // }
    //
    // private Vector2[] RotateRectangle(Vector2[] corners, Vector2 datumPoint, double angle)
    // {
    //     for (int i = 0; i < corners.Length; i++)
    //     {
    //         Vector2 offset = corners[i] - datumPoint;
    //         float x = offset.X;
    //         float z = offset.Y;
    //         corners[i] = new Vector2(
    //             datumPoint.X + x * (float)Math.Cos(angle) - z * (float)Math.Sin(angle),
    //             datumPoint.Y + x * (float)Math.Sin(angle) + z * (float)Math.Cos(angle)
    //         );
    //     }
    //
    //     return corners;
    // }
    
    public GameObject? FindDensityTargets(List<GameObjectType> searchType, List<GameObjectType> targetType,
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

    #region Summary
    /// <summary>
    /// Find multiple targets in the range of dist from GameObject
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="typeList"></param>
    /// <param name="dist"></param>
    /// <param name="attackType"></param>
    /// <returns>"plural targets in the range."</returns>
    #endregion
    public List<GameObject> FindTargets(
        GameObject gameObject, IEnumerable<GameObjectType> typeList, float dist = 100, int attackType = 0)
    {
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
        var objectsInDist = targetList
            .Where(obj => obj.Targetable && (attackType == 2 || obj.UnitType == attackType))
            .Where(obj => Vector3.Distance(
                obj.CellPos with { Y = 0 }, gameObject.CellPos with { Y = 0 }) < dist)
            .ToList();

        return objectsInDist;
    }

    #region Summary
    /// <summary>
    /// Find multiple targets in the range of dist from specific CellPos
    /// </summary>
    /// <param name="cellPos"></param>
    /// <param name="typeList"></param>
    /// <param name="dist"></param>
    /// <param name="attackType"></param>
    /// <returns>"plural targets in the range."</returns>
    #endregion
    public List<GameObject> FindTargets(
        Vector3 cellPos, IEnumerable<GameObjectType> typeList, float dist = 100, int attackType = 0)
    {
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
        var objectsInDist = targetList
            .Where(obj => obj.Stat.Targetable && (attackType == 2 || obj.UnitType == attackType))
            .Where(obj => Vector3.Distance(obj.CellPos with { Y = 0 }, cellPos with { Y = 0 }) < dist)
            .ToList();

        return objectsInDist;
    }
    
    #region Summary
    /// <summary>
    /// Find multiple targets within a certain angle range from the GameObject
    /// </summary>
    /// <param name="gameObject">The GameObject from which the angle is measured</param>
    /// <param name="typeList">The types of GameObjects to search for</param>
    /// <param name="skillDist">The distance within which to search for targets</param>
    /// <param name="angle">The half-angle range (in degrees) to search for targets</param>
    /// <param name="attackType">The specific target type to search for</param>
    /// <returns>List of GameObjects within the angle range</returns>
    #endregion
    public List<GameObject> FindTargetsInAngleRange(GameObject gameObject, 
        IEnumerable<GameObjectType> typeList, float skillDist = 100, float angle = 30, int attackType = 0)
    {   // 1. 타겟 리스트 생성
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
        // 2. 현재 위치와 전방 벡터 계산
        double radians = gameObject.Dir * (Math.PI / 180);
        Vector3 forward = new Vector3((int)Math.Round(Math.Sin(radians)), 0, (int)Math.Round(Math.Cos(radians)));
      
        // 3. 필터링할 목표물 리스트 초기화
        var objectsInAngleRange = new List<GameObject>();

        // 4. 각 목표물에 대해 조건 검사
        foreach (var obj in targetList)
        {   // 4.1 타겟팅 가능한지와 공격 타입이 맞는지 확인
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
            {   // 조건에 맞으면 리스트에 추가
                objectsInAngleRange.Add(obj);
            }
        }

        return objectsInAngleRange;
    }

    #region Summary

    /// <summary>
    /// Find multiple targets with specific unitIds or species in the range of dist from GameObject
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="type"></param>
    /// <param name="unitIds"></param>
    /// <param name="dist"></param>
    /// <returns></returns>

    #endregion
    public List<GameObject> FindTargetsBySpecies(GameObject gameObject, 
        GameObjectType type, IEnumerable<UnitId> unitIds, float dist = 100)
    {
        var targetList = GetTargets(type).ToList();
        if (targetList.Count == 0) return new List<GameObject>();

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
        PositionInfo posInfo = new PositionInfo()
        {
            PosX = statue.PosInfo.PosX,
            PosY = statue.PosInfo.PosY,
            PosZ = statue.PosInfo.PosZ,
            State = State.Idle,
        };
        
        if (statue.Way == SpawnWay.North)
        {
            posInfo.PosZ = statue.PosInfo.PosZ - statue.Stat.SizeZ;
        }
        else
        {
            posInfo.PosZ = statue.PosInfo.PosZ + statue.Stat.SizeZ;
        }

        var pos = new Vector3(posInfo.PosX, posInfo.PosY, posInfo.PosZ);
        pos = Map.Vector2To3(Map.FindNearestEmptySpace(Map.Vector3To2(pos), statue));
        posInfo.PosX = pos.X;
        posInfo.PosY = pos.Y;
        posInfo.PosZ = pos.Z;
        
        return posInfo;
    }
    
    public GameObject? FindMosquitoInFence()
    {
        foreach (var monster in _monsters.Values)
        {
            if (monster.UnitId is not 
                (UnitId.MosquitoBug or UnitId.MosquitoPester or UnitId.MosquitoStinger)) continue;

            if (InsideFence(monster))
            {
                return monster;
            }
        }
        
        return null;
    }

    private Dictionary<int, GameObject> AddTargetType(GameObjectType type)
    {
        Dictionary<int, GameObject> targetDict = new();
        switch (type)
        {
            case GameObjectType.Monster:
                foreach (var (key, value) in _monsters) targetDict.Add(key, value);
                break;
            case GameObjectType.Tower:
                foreach (var (key, value) in _towers) targetDict.Add(key, value);
                break;
            case GameObjectType.Fence:
                foreach (var (key, value) in _fences) targetDict.Add(key, value);
                break;
            case GameObjectType.Sheep:
                foreach (var (key, value) in _sheeps) targetDict.Add(key, value);
                break;
        }

        return targetDict.Count != 0 ? targetDict : new Dictionary<int, GameObject>();
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
    
    private bool InsideFence(GameObject gameObject)
    {
        Vector3 cell = gameObject.CellPos;
        Vector3 center = GameData.FenceCenter;
        Vector3 size = GameData.FenceSize;

        float halfWidth = size.X / 2;
        float minX = center.X - halfWidth;
        float maxX = center.X + halfWidth;
        float halfHeight = size.Z / 2;
        float minZ = center.Z - halfHeight;
        float maxZ = center.Z + halfHeight;

        bool insideX = minX <= cell.X && maxX >= cell.X;
        bool insideZ = minZ <= cell.Z && maxZ >= cell.Z;
        
        return insideX && insideZ;
    }
}