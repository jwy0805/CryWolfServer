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
        // Spawn Rock Pile
        Vector3[] portalPos = GameData.SpawnerPos;

        Portal northPortal = ObjectManager.Instance.Add<Portal>();
        northPortal.Init();
        northPortal.Way = SpawnWay.North;
        northPortal.Info.Name = "Portal#6Red";
        northPortal.CellPos = portalPos[0];
        northPortal.Dir = 90;
        Push(EnterGame, northPortal);
        
        Portal southPortal = ObjectManager.Instance.Add<Portal>();
        southPortal.Init();
        southPortal.Way = SpawnWay.South;
        southPortal.Info.Name = "Portal#6Blue";
        southPortal.CellPos = portalPos[1];
        southPortal.Dir = 270;
        Push(EnterGame, southPortal);
    }
    
    public void InfoInit()
    {
        GameInfo = new GameInfo(_players);
        StorageLevel = 1;
        foreach (var player in _players.Values)
        {
            EnterSheepByServer(player);
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

        var targetTypeList = GetTargetType(gameObject);
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

    private List<GameObjectType> GetTargetType(GameObject gameObject)
    {
        List<GameObjectType> targetTypeList = new();
        switch (gameObject.ObjectType)
        {
            case GameObjectType.Monster:
                if (ReachableInFence(gameObject))
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
            if (target.Stat.Targetable == false || target.Id == gameObject.Id
                || (target.UnitType != attackType && attackType != 2)) continue;
            var pos = target.PosInfo;
            var targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            var distSq = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (distSq < closestDistSq == false) continue;
            closest = target;
            closestDistSq = distSq;
        }

        return closest;
    }
    
    public List<GameObject> FindTargetsInRectangle(List<GameObjectType> typeList, GameObject gameObject, double width, double height, int targetType)
    {
        Map map = Map;
        Pos pos = map.Cell2Pos(map.Vector3To2(gameObject.CellPos));
        
        double halfWidth = width / 2.0f;
        double angle = gameObject.Dir * Math.PI / 180;

        double x1 = pos.X - halfWidth;
        double x2 = pos.X + halfWidth;
        double z1 = pos.Z - height;
        double z2 = pos.Z;
        Vector2[] corners = new Vector2[4];
        corners[0] = new Vector2((float)x1, (float)z1);
        corners[1] = new Vector2((float)x1, (float)z2);
        corners[2] = new Vector2((float)x2, (float)z2);
        corners[3] = new Vector2((float)x2, (float)z1);
        
        List<GameObject> gameObjects = FindTargets(gameObject, typeList, (float)(height > width ? height : width));
        Vector2[] cornersRotated = RotateRectangle(corners, new Vector2(pos.X, pos.Z), angle);

        return (from obj in gameObjects 
            where obj.Stat.Targetable && targetType == 2 || obj.UnitType == targetType
            let objPos = map.Cell2Pos(map.Vector3To2(obj.CellPos)) 
            let point = new Vector2(objPos.X, objPos.Z) 
            where CheckPointInRectangle(cornersRotated, point, width * height) select obj).ToList();
    }

    public List<GameObject> FindTargetsInCone( List<GameObjectType> typeList, GameObject gameObject, float angle, float dist = 100, int targetType = 0)
    {
        Dictionary<int, GameObject> targetDict = new();
        targetDict = typeList.Select(AddTargetType)
            .Aggregate(targetDict, (current, dictionary) 
                => current.Concat(dictionary).ToDictionary(pair => pair.Key, pair => pair.Value));
        
        if (targetDict.Count == 0) return new List<GameObject>();
        
        Vector2Int currentPos = Map.Vector3To2(gameObject.CellPos);
        Vector2Int upVector = new Vector2Int(0, 1);

        var targets = targetDict.Values.Select(obj => new 
            { 
                Object = obj, 
                Position = Map.Vector3To2(obj.CellPos),
                Direction = Map.Vector3To2(obj.CellPos) - currentPos
            });

        var targetsInDist = targets
            .Where(t => t.Object.Stat.Targetable && targetType == 2 || t.Object.UnitType == targetType)
            .Where(t => t.Direction.Magnitude < dist)
            .Where(t => Math.Abs(Vector2Int.SignedAngle(upVector, t.Direction, gameObject.Dir)) <= angle / 2)
            .Select(t => t.Object)
            .ToList();

        return targetsInDist;
    }
    
    public GameObject? FindTargetWithManyFriends(List<GameObjectType> type, List<GameObjectType> typeList, 
        GameObject gameObject, float skillRange = 5, int targetType = 2)
    {
        List<GameObject> targets = FindTargets(gameObject, type, 100, 2)
            .Where(t => t.Way == gameObject.Way || t.Way == SpawnWay.Any).ToList();
        List<GameObject> allTargets = FindTargets(gameObject, typeList, 100, 2)
            .Where(t => t.Way == gameObject.Way || t.Way == SpawnWay.Any).ToList();
        if (targets.Count == 0) return null;

        GameObject target = new GameObject();
        int postFriendsCnt = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            int friendsCnt = 0;
            for (int j = 0; j < allTargets.Count; i++)
            {
                Vector3 friendPos = allTargets[j].CellPos;
                bool targetable = allTargets[j].Stat.Targetable;
                float dist = new Vector3().SqrMagnitude(friendPos - gameObject.CellPos);
                if (dist < skillRange * skillRange && targetable) friendsCnt++;
            }
            
            if (friendsCnt > postFriendsCnt)
            {
                postFriendsCnt = friendsCnt;
                target = targets[i];
            }
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
    /// <param name="targetType"></param>
    /// <returns>"plural targets in the range."</returns>
    #endregion
    public List<GameObject> FindTargets(GameObject gameObject, IEnumerable<GameObjectType> typeList, float dist = 100, int targetType = 0)
    {
        var targetList = typeList.SelectMany(GetTargets).ToList();
        if (targetList.Count == 0) return new List<GameObject>();
        
        var objectsInDist = targetList
            .Where(obj => obj.Stat.Targetable && targetType == 2 || obj.UnitType == targetType)
            .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < dist * dist)
            .ToList();

        return objectsInDist;
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
    public List<GameObject> FindTargetsBySpecies(GameObject gameObject, GameObjectType type, IEnumerable<UnitId> unitIds, float dist = 100)
    {
        var targetList = GetTargets(type).ToList();
        if (targetList.Count == 0) return new List<GameObject>();

        var objectsInDist = new List<GameObject>();
        switch (type)
        {
            case GameObjectType.Monster:
                objectsInDist = targetList.OfType<Monster>()
                    .Where(obj => unitIds.Contains(obj.UnitId))
                    .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < dist * dist)
                    .Cast<GameObject>()
                    .ToList();
                break;
            case GameObjectType.Tower:
                objectsInDist = targetList.OfType<Tower>()
                    .Where(obj => unitIds.Contains(obj.UnitId))
                    .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < dist * dist)
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
    
    private double GetAreaOfTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        return Math.Abs((a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y)) / 2);
    }

    private bool CheckPointInRectangle(Vector2[] corners, Vector2 point, double area)
    {
        Vector2 a = corners[0];
        Vector2 b = corners[1];
        Vector2 c = corners[2];
        Vector2 d = corners[3];
        
        double area1 = GetAreaOfTriangle(a, b, point);
        double area2 = GetAreaOfTriangle(b, c, point);
        double area3 = GetAreaOfTriangle(c, d, point);
        double area4 = GetAreaOfTriangle(d, a, point);
        double sum = area1 + area2 + area3 + area4;

        return Math.Abs(sum - area) < 0.01f;
    }
    
    private Vector2[] RotateRectangle(Vector2[] corners, Vector2 datumPoint, double angle)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 offset = corners[i] - datumPoint;
            float x = offset.X;
            float z = offset.Y;
            corners[i] = new Vector2(
                datumPoint.X + x * (float)Math.Cos(angle) - z * (float)Math.Sin(angle),
                datumPoint.Y + x * (float)Math.Sin(angle) + z * (float)Math.Cos(angle)
            );
        }

        return corners;
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