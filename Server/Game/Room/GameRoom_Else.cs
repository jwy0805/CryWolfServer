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
        var northPortal = ObjectManager.Instance.Add<Portal>();
        northPortal.Init();
        northPortal.Way = SpawnWay.North;
        northPortal.Info.Name = "Portal#6Red";
        northPortal.CellPos = GameData.PortalPos[0];
        northPortal.Dir = 90;
        Push(EnterGame, northPortal);
    }
    
    public void InfoInit()
    {
        GameInfo = new GameInfo(_players, MapId)
        {
            FenceCenter = GameData.InitFenceCenter,
            FenceStartPos = GameData.InitFenceStartPos,
            FenceSize = GameData.InitFenceSize,
        };
        
        StorageLevel = 1;
        
        // Spawn Prime Sheep
        var sheepPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Sheep);
        if (sheepPlayer != null)
        {
            SpawnPrimeSheep((SheepId)sheepPlayer.AssetId, sheepPlayer);
        }
        
        // Set Enchant
        var wolfPlayer = _players.Values.FirstOrDefault(player => player.Faction == Faction.Wolf);
        if (wolfPlayer != null)
        {
            Enchant = EnchantManager.Instance.CreateEnchant((EnchantId)wolfPlayer.AssetId);
            Enchant.Room = this;
            Enchant.Update();
        }
        
        // Set UI Text
        foreach (var player in _players.Values)
        {
            if (player.Session == null) continue;
            
            if (player.Faction == Faction.Sheep)
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.SheepResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthTower, Max = false });
                if (MapId != 1)
                {
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxTower, Max = true });
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthTower, Max = false });
                }
            }
            else
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.WolfResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMonster, Max = false });
                if (MapId != 1)
                {
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxMonster, Max = true });
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMonster, Max = false });
                }
            }
        }
    }

    /// <summary>
    /// Find a Nearest Target.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="attackType"></param>
    /// <returns>a Nearest Target GameObject</returns>
    public GameObject? FindClosestTarget(GameObject gameObject, int attackType = 0)
    {   
        // 어그로 끌린 상태면 리턴
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target; 

        var targetTypeList = GetTargetType(gameObject, ReachableInFence(gameObject));
        var targetList = new List<GameObject>();
        foreach (var type in targetTypeList) targetList.AddRange(GetTargets(type));

        return MeasureShortestDist(gameObject, targetList, attackType);
    }
    
    public GameObject? FindClosestPriorityTarget(GameObject gameObject, List<GameObjectType> typeList, int attackType = 0)
    {
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        foreach (var type in typeList)
        {
            var targetList = GetTargets(type).ToList();
            var finalTarget = MeasureShortestDist(gameObject, targetList, attackType);
            if (finalTarget != null) return finalTarget;
        } 

        return null;
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
                    {
                        GameObjectType.Tower, GameObjectType.Sheep
                    };
                }
                else
                {
                    targetTypeList = new List<GameObjectType>
                    {
                        GameObjectType.Tower, GameObjectType.Sheep, GameObjectType.Fence
                    };
                }
                break;
            case GameObjectType.Tower:
                targetTypeList = new List<GameObjectType>
                {
                    GameObjectType.Monster, GameObjectType.MonsterStatue, GameObjectType.Portal
                };
                break;
        }
        
        return targetTypeList;
    }

    private bool ReachableInFence(GameObject gameObject)
    {
        var type = gameObject.ObjectType;
        if (type is GameObjectType.Tower or GameObjectType.Sheep) return true;
        if (type is GameObjectType.Monster && gameObject.Stat.UnitType == 1) return true;

        // var sheep = FindNearestSheep(gameObject);
        // if (sheep == null) return false;
        // var destCell = Map.Vector3To2(Map.GetClosestPoint(gameObject, sheep));
        // var path = Map.GetPath(gameObject, true, destCell);
        // Console.WriteLine(path.Count);
        // return GameInfo.NorthFenceCnt < GameInfo.NorthMaxFenceCnt && path.Count != 0;
        return GameInfo.NorthFenceCnt < GameInfo.NorthMaxFenceCnt;
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
            case GameObjectType.Portal:
                targets = _portals.Values.Cast<GameObject>().ToList();
                break;
        }

        return targets;    
    }
    
    private GameObject? MeasureShortestDist(GameObject gameObject, List<GameObject> targets, int attackType)
    {
        PriorityQueue<TargetDistance, float> pq = new();
        
        foreach (var target in targets)
        {
            if (target.Targetable == false || target.Id == gameObject.Id
                || (target.UnitType != attackType && attackType != 2)) continue;
            
            var targetPos = Map.Vector2To3(Map.FindNearestEmptySpace(
                Map.Vector3To2(Map.GetClosestPoint(gameObject, target)), gameObject));
            targetPos = targetPos with { Y = 0 };
            var cellPos = gameObject.CellPos with { Y = 0 };
            var distance = Vector3.Distance(targetPos, cellPos);
            pq.Enqueue(new TargetDistance { Target = target, Distance = distance }, distance);
        }
        
        if (pq.Count == 0 || gameObject is not Creature creature) return null;
        
        while (pq.Count > 0)
        {
            var target = pq.Dequeue().Target;
            if (creature.UnreachableIds.Contains(target.Id))
            {
                Vector2Int startCell = Map.Vector3To2(gameObject.CellPos);
                Vector2Int destCell = Map.Vector3To2(Map.GetClosestPoint(gameObject, target));
                var path = Map.FindPath(gameObject, startCell, destCell);
                if (path.Count != 0)
                {
                    creature.UnreachableIds.Clear();
                    return target;
                }
            }
            else
            {
                return target;
            }
        }

        return null;
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
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
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
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
        var objectsInDist = targetList
            .Where(obj => obj.Stat.Targetable && (attackType == 2 || obj.UnitType == attackType))
            .Where(obj => Vector3.Distance(obj.CellPos with { Y = 0 }, cellPos with { Y = 0 }) < dist)
            .ToList();

        return objectsInDist;
    }
    
    /// <summary>
    /// Find multiple targets within a certain angle range from the GameObject
    /// </summary>
    /// <param name="gameObject">The GameObject from which the angle is measured</param>
    /// <param name="typeList">The types of GameObjects to search for</param>
    /// <param name="skillDist">The distance within which to search for targets</param>
    /// <param name="angle">The half-angle range (in degrees) to search for targets</param>
    /// <param name="attackType">The specific target type to search for</param>
    /// <returns>List of GameObjects within the angle range</returns>
    public List<GameObject> FindTargetsInAngleRange(GameObject gameObject, float dir,
        IEnumerable<GameObjectType> typeList, float skillDist = 100, float angle = 30, int attackType = 0)
    {   
        // 1. 타겟 리스트 생성
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
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
        pos = Map.Vector2To3(Map.FindNearestEmptySpace(Map.Vector3To2(pos), statue));
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