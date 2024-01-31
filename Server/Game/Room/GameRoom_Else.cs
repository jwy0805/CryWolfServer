using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public partial class GameRoom
{
    private void BaseInit()
    {
        StorageLevel = 1;

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
        foreach (var player in _players.Values)
        {
            if (player.Camp == Camp.Sheep)
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.SheepResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.MaxSheep, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.Sheep, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthTower, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthTower, Max = false });
            }
            else
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.WolfResource, Max = false});
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.MaxSheep, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = GameInfo.Sheep, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMonster, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMonster, Max = false });
            }
        }
    }
    
    public GameObject? FindNearestTarget(GameObject gameObject, int attackType = 0)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        if (gameObject.Buffs.Contains(BuffId.Aggro)) return gameObject.Target;

        List<GameObjectType> targetType = new();
        switch (gameObject.ObjectType)
        {
            case GameObjectType.Monster:
                if (ReachableInFence())
                {
                    targetType = new List<GameObjectType>
                        { GameObjectType.Fence, GameObjectType.Tower, GameObjectType.Sheep };
                }
                else
                {
                    targetType = new List<GameObjectType> { GameObjectType.Fence, GameObjectType.Tower };
                }
                break;
            case GameObjectType.Tower:
                targetType = new List<GameObjectType> { GameObjectType.Monster };
                break;
        }
        
        Dictionary<int, GameObject> targetDict = new();

        foreach (var t in targetType)
        {
            switch (t)
            {
                case GameObjectType.Monster:
                    foreach (var (key, monster) in _monsters) targetDict.Add(key, monster);
                    break;
                case GameObjectType.Tower:
                    foreach (var (key, tower) in _towers) targetDict.Add(key, tower);
                    break;
                case GameObjectType.Sheep:
                    foreach (var (key, sheep) in _sheeps) targetDict.Add(key, sheep);
                    break;
                case GameObjectType.Fence:
                    foreach (var (key, fence) in _fences) targetDict.Add(key, fence);
                    break;
            }
        }

        if (gameObject.ObjectType == GameObjectType.Monster && ReachableInFence())
        {   // 울타리가 뚫렸을 때 타겟 우선순위 = 1. 양, 타워 -> 2. 울타리
            List<int> keysToRemove = new List<int>();
            if (targetDict.Values.Any(go => go.ObjectType != GameObjectType.Fence))
            {
                keysToRemove.AddRange(from pair in targetDict 
                    where pair.Value.ObjectType == GameObjectType.Fence select pair.Key);
                foreach (var key in keysToRemove) targetDict.Remove(key);
            }
        }
        
        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        foreach (var go in targetDict.Values)
        {
            PositionInfo pos = go.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = go.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable && go.Id != gameObject.Id && (go.UnitType == attackType || attackType == 2))
            {
                closestDist = dist;
                target = go;
            }
        }

        return target;
    }
    
    public GameObject? FindNearestTarget(GameObject gameObject, List<GameObjectType> typeList, int attackType = 0)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        if (BuffManager.Instance.Buffs.Select(b => b)
            .Any(buff => buff.Id == BuffId.Aggro && buff.Master.Id == gameObject.Id)) 
            return gameObject.Target;

        Dictionary<int, GameObject> targetDict = new();

        foreach (var t in typeList)
        {
            switch (t)
            {
                case GameObjectType.Monster:
                    foreach (var (key, monster) in _monsters) targetDict.Add(key, monster);
                    break;
                case GameObjectType.Tower:
                    foreach (var (key, tower) in _towers) targetDict.Add(key, tower);
                    break;
                case GameObjectType.Sheep:
                    foreach (var (key, sheep) in _sheeps) targetDict.Add(key, sheep);
                    break;
                case GameObjectType.Fence:
                    foreach (var (key, fence) in _fences) targetDict.Add(key, fence);
                    break;
            }
        }

        if (gameObject.ObjectType == GameObjectType.Monster && ReachableInFence())
        {   // 울타리가 뚫렸을 때 타겟 우선순위 = 1. 양, 타워 -> 2. 울타리
            List<int> keysToRemove = new List<int>();
            if (targetDict.Values.Any(go => go.ObjectType != GameObjectType.Fence))
            {
                keysToRemove.AddRange(from pair in targetDict 
                    where pair.Value.ObjectType == GameObjectType.Fence select pair.Key);
                foreach (var key in keysToRemove) targetDict.Remove(key);
            }
        }
        
        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        foreach (var go in targetDict.Values)
        {
            PositionInfo pos = go.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = go.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable && go.Id != gameObject.Id && (go.UnitType == attackType || attackType == 2))
            {
                closestDist = dist;
                target = go;
            }
        }

        return target;
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
    
    public GameObject? FindNearestTower(List<TowerId> towerIdList)
    {
        Dictionary<int, GameObject> targetDict = new();
        foreach (var towerId in towerIdList)
        {
            foreach (var (key, tower) in _towers)
            {
                if (tower.TowerId == towerId) targetDict.Add(key, tower);
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

    public List<GameObject> FindTargets(GameObject gameObject, List<GameObjectType> typeList, float dist = 100, int targetType = 0)
    {
        Dictionary<int, GameObject> targetDict = new();
        targetDict = typeList.Select(AddTargetType)
            .Aggregate(targetDict, (current, dictionary) 
                => current.Concat(dictionary).ToDictionary(pair => pair.Key, pair => pair.Value));
        
        if (targetDict.Count == 0) return new List<GameObject>();
        
        List<GameObject> objectsInDist = targetDict.Values
            .Where(obj => obj.Stat.Targetable && targetType == 2 || obj.UnitType == targetType)
            .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < dist * dist)
            .ToList();
        
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
        
        return posInfo;
    }
    
    public GameObject? FindMosquitoInFence()
    {
        foreach (var monster in _monsters.Values)
        {
            if (monster.MonsterId is not 
                (MonsterId.MosquitoBug or MonsterId.MosquitoPester or MonsterId.MosquitoStinger)) continue;

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

    private bool ReachableInFence()
    {
        return _fences.Count < GameData.NorthFenceMax;
    }

    private bool CanUpgradeTower(Player player, TowerId towerId)
    {
        return true;
    }
    
    private bool CanUpgradeMonster(Player player, MonsterId monsterId)
    {
        return true;
    }
    
    private void ProcessingBaseSkill(Player player)
    {
        
    }
}