using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public partial class GameRoom
{
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
        foreach (var (key, obj) in targetDict)
        {
            PositionInfo pos = obj.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = obj.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable && obj.Id != gameObject.Id && (obj.UnitType == attackType || attackType == 2))
            {
                closestDist = dist;
                target = obj;
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
        foreach (var (key, obj) in targetDict)
        {
            PositionInfo pos = obj.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = obj.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable && obj.Id != gameObject.Id && (obj.UnitType == attackType || attackType == 2))
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }

    public List<GameObject> FindBuffTargets(GameObject gameObject, List<GameObjectType> typeList, float dist)
    {
        Dictionary<int, GameObject> targetDict = new();
        targetDict = typeList.Select(AddTargetType)
            .Aggregate(targetDict, (current, dictionary) 
                => current.Concat(dictionary).ToDictionary(pair => pair.Key, pair => pair.Value));
        
        if (targetDict.Count == 0) return new List<GameObject>();

        List<GameObject> objectsInDist = targetDict.Values
            .Where(obj => obj.Stat.Targetable)
            .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < dist * dist)
            .ToList();
        
        return objectsInDist;
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
            case GameObjectType.Projectile:
                if (_projectiles.TryGetValue(id, out var projectile)) go = projectile;
                break;
            default:
                go = null;
                break;
        }

        return go;
    }
    

    private bool InsideFence(GameObject gameObject)
    {
        int lv = StorageLevel;
        Vector3 cell = gameObject.CellPos;
        Vector3 center = GameData.FenceCenter[lv];
        Vector3 size = GameData.FenceSize[lv];

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
        return _fences.Count < GameData.FenceCnt[_storageLevel];
    }

    private bool CanUpgradeSkill(Player player, Skill skill)
    {
        Skill[] skills = GameData.SkillTree[skill];
        int resource = player.Resource;

        if (skills.All(item => player.SkillUpgradedList.Contains(item)))
        {
            return true;
        }
        
        return false;
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