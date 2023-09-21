using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    public readonly Stopwatch Stopwatch = new();
    
    public int StorageLevel
    {
        get => _storageLevel;
        set
        {
            _storageLevel = value;
            if (_storageLevel > 3) _storageLevel = 3;
            GameData.StorageLevel = _storageLevel;
            
            // 울타리 생성
            if (_storageLevel != 1 && _fences.Count > 0)
            {
                foreach (var fenceId in _fences.Keys) Push(LeaveGame, fenceId);
            }

            Vector3 center = GameData.FenceCenter[_storageLevel];
            Vector3 size = GameData.FenceSize[_storageLevel];
            GameData.FenceBounds = new List<Vector3>
            {
                new Vector3(center.X - size.X / 2 , 6, center.Z + size.Z / 2),
                new Vector3(center.X - size.X / 2 , 6, center.Z - size.Z / 2),
                new Vector3(center.X + size.X / 2 , 6, center.Z - size.Z / 2),
                new Vector3(center.X + size.X / 2 , 6, center.Z + size.Z / 2)
            };
            
            SpawnFence(_storageLevel);
        }
    }
    
    private void GameInit()
    {
        Stopwatch.Start();
        StorageLevel = 1;
        BuffManager.Instance.Room = this;
        BuffManager.Instance.Update();
    }
    
    public void HandlePlayerMove(Player? player, C_PlayerMove pMovePacket)
    {
        if (player == null) return;
        
        S_PlayerMove playerMovePacket = new S_PlayerMove
        {
            State = pMovePacket.State,
            ObjectId = player.Id,
            DestPos = pMovePacket.DestPos
        }; 
        
        Broadcast(playerMovePacket);
    }
    
    public void HandleMove(Player? player, C_Move movePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(movePacket.ObjectId);
        if (go == null) return;
        Vector3 v = new Vector3(movePacket.PosX, movePacket.PosY, movePacket.PosZ);
        Vector3 cellPos = Util.Util.NearestCell(v);
        go.ApplyMap(cellPos);
    }

    public void HandleSetDest(Player? player, C_SetDest destPacket)
    {
        if (player == null) return;
        GameObjectType type = ObjectManager.GetObjectTypeById(destPacket.ObjectId);
        if (type == GameObjectType.Projectile)
        {
            Projectile? p = FindGameObjectById(destPacket.ObjectId) as Projectile;
            p?.BroadcastDest();
        }
        else
        {
            GameObject? go = FindGameObjectById(destPacket.ObjectId);
            go?.BroadcastDest();
        }
    }
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket)
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;

        switch (type)
        {
            case GameObjectType.Tower:
                if (!Enum.IsDefined(typeof(TowerId), spawnPacket.Num)) return;
                TowerId towerType = (TowerId)spawnPacket.Num;
                Tower tower = ObjectManager.Instance.CreateTower(towerType);
                tower.PosInfo = spawnPacket.PosInfo;
                tower.Info.PosInfo = tower.PosInfo;
                tower.TowerNum = spawnPacket.Num;
                tower.Player = player;
                tower.TowerId = towerType;
                tower.Room = this;
                tower.Init();
                Push(EnterGame, tower);
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                MonsterId monsterType = (MonsterId)spawnPacket.Num;
                Monster monster = ObjectManager.Instance.CreateMonster(monsterType);
                monster.PosInfo = spawnPacket.PosInfo;
                monster.CellPos = Map.FindSpawnPos(monster, spawnPacket.Way);
                monster.Info.PosInfo = monster.PosInfo;
                monster.MonsterNum = spawnPacket.Num;
                monster.Player = player;
                monster.MonsterId = monsterType;
                monster.Room = this;
                monster.Init();
                Push(EnterGame, monster);
                break;
            
            case GameObjectType.Sheep:
                Sheep sheep = ObjectManager.Instance.Add<Sheep>();
                sheep.PosInfo = spawnPacket.PosInfo;
                sheep.CellPos = Map.FindSpawnPos(sheep, SpawnWay.Any);
                sheep.Info.PosInfo = sheep.PosInfo;
                sheep.Player = player;
                sheep.Init();
                Push(EnterGame, sheep);
                break;
            
            case GameObjectType.Effect:
                EffectId effectType = (EffectId)spawnPacket.Num;
                Effect effect = ObjectManager.Instance.CreateEffect(effectType);
                effect.PosInfo = spawnPacket.PosInfo;
                effect.Info.PosInfo = effect.PosInfo;
                effect.Init();
                Push(EnterGame, effect);
                break;
        }
    }
    
    private void SpawnFence(int storageLv = 1)
    {
        Vector3[] fencePos = GameData.GetPos(GameData.FenceCnt[storageLv], GameData.FenceRow[storageLv], GameData.FenceStartPos[storageLv]);
        float[] fenceRotation = GameData.GetRotation(GameData.FenceCnt[storageLv], GameData.FenceRow[storageLv]);

        for (int i = 0; i < GameData.FenceCnt[storageLv]; i++)
        {
            Fence fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceName[storageLv];
            fence.CellPos = fencePos[i];
            fence.Dir = fenceRotation[i];
            Push(EnterGame, fence);
        }
    }

    public void HandleState(Player? player, C_State statePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(statePacket.ObjectId);
        if (go == null) return;
        go.State = statePacket.State;
    }
    
    public void HandleAttack(Player? player, C_Attack attackPacket)
    {
        if (player == null) return;
        int attackerId = attackPacket.ObjectId;
        GameObject? attacker = FindGameObjectById(attackerId);
        GameObject? target = attacker?.Target;
        if (attacker == null || target == null) return;
        if (target.Targetable == false) return;
        GameObjectType type = attacker.ObjectType;
        
        switch (attackPacket.AttackMethod)
        {
            case AttackMethod.NormalAttack:
                int damage = attacker.TotalAttack;
                target.OnDamaged(attacker, damage);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                    cAttacker.Mp += cAttacker.Stat.MpRecovery;
                    cAttacker.SetNormalAttackEffect(target);
                }
                else if (type is GameObjectType.Effect)
                {
                    attacker.Parent!.Mp += attacker.Parent.Stat.MpRecovery;
                    Effect? eAttacker = FindGameObjectById(attackerId) as Effect;
                    eAttacker?.SetEffectEffect(target);
                }
                else if (type is GameObjectType.Projectile)
                {
                    attacker.Parent!.Mp += attacker.Parent.Stat.MpRecovery;
                    Projectile? pAttacker = FindGameObjectById(attackerId) as Projectile;
                    pAttacker?.SetProjectileEffect(target);
                }
                break;
            
            case AttackMethod.EffectAttack:
                if (!Enum.IsDefined(typeof(EffectId), attackPacket.Effect)) return;
                int skillDamage = attacker.SkillDamage;
                Effect effect = ObjectManager.Instance.CreateEffect(attackPacket.Effect);
                effect.Room = this;
                effect.Parent = attacker;
                effect.PosInfo = target.PosInfo;
                effect.Info.PosInfo = target.Info.PosInfo;
                effect.Info.Name = attackPacket.Effect.ToString();
                effect.EffectId = attackPacket.Effect;
                effect.Init();
                target.OnDamaged(attacker, skillDamage);
                Push(EnterGame, effect);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                }
                break;
            
            case AttackMethod.ProjectileAttack:
                if (!Enum.IsDefined(typeof(ProjectileId), attackPacket.Projectile)) return;
                Projectile projectile = ObjectManager.Instance.CreateProjectile(attackPacket.Projectile);
                projectile.Room = this;
                projectile.PosInfo = attacker.PosInfo;
                // projectile.PosInfo.PosY = attacker.PosInfo.PosY + attacker.Stat.SizeY;
                projectile.Info.PosInfo = projectile.PosInfo;
                projectile.Info.Name = attackPacket.Projectile.ToString();
                projectile.ProjectileId = attackPacket.Projectile;
                projectile.Target = target;
                projectile.Parent = attacker;
                projectile.TotalAttack = attacker.TotalAttack;
                projectile.Init();
                Push(EnterGame, projectile);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                }
                break;
        }
    }

    public void HandleStatInit(Player? player, C_StatInit initPacket)
    {
        if (player == null) return;

        Creature? creature = FindGameObjectById(initPacket.ObjectId) as Creature;
        creature?.StatInit();
    }

    public void HandleSkill(Player? player, C_Skill skillPacket)
    {
        if (player == null) return;

        Creature? creature = FindGameObjectById(skillPacket.ObjectId) as Creature;
        creature?.RunSkill();
        creature?.SetNextState();
    }

    public void HandleSkillUpgrade(Player? player, C_SkillUpgrade upgradePacket)
    {
        if (player == null) return;
        
        Skill skill = upgradePacket.Skill;
        bool canUpgrade = CanUpgradeSkill(player, skill);
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> "cost가 부족합니다"
            return;
        }
        
        if (Enum.IsDefined(typeof(Skill), skill.ToString()))
            player.SkillSubject.SkillUpgraded(skill);
        else ProcessingBaseSkill(player);
        
        player.SkillUpgradedList.Add(skill);
        player.SkillSubject.Notify();
        player.Session.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
    {
        if (player == null) return;

        bool canUpgrade = false;
        TowerId towerId = TowerId.UnknownTower;
        MonsterId monsterId = MonsterId.UnknownMonster;
        if (upgradePacket.MonsterId == MonsterId.UnknownMonster)
        {
            towerId = upgradePacket.TowerId;
            canUpgrade = CanUpgradeTower(player, towerId);
        }
        else
        {
            monsterId = upgradePacket.MonsterId;
            canUpgrade = CanUpgradeMonster(player, monsterId);
        }
        
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> cost 부족
        }
        else
        {
            if (monsterId == MonsterId.UnknownMonster) towerId = (TowerId)((int)towerId + 1);
            else monsterId = (MonsterId)((int)monsterId + 1);
            player.Session.Send(new S_UnitUpgrade { TowerId = towerId, MonsterId = monsterId });
        }
    }

    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
    
    #region Find

    public GameObject? FindNearestTarget(GameObject gameObject)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        // foreach (BuffManager.IBuff b in BuffManager.Instance.Buffs)
        // {
        //     BuffManager.ABuff? buff = b as BuffManager.ABuff;
        //     if (buff?.Id == BuffId.Aggro && buff.Master.Id == gameObject.Id) return gameObject.Target;
        // }    
        if (BuffManager.Instance.Buffs.Select(b => b as BuffManager.ABuff)
            .Any(buff => buff?.Id == BuffId.Aggro && buff.Master.Id == gameObject.Id)) 
            return gameObject.Target;

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
            if (dist < closestDist && targetable && obj.Id != gameObject.Id)
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }
    
    public GameObject? FindNearestTarget(GameObject gameObject, List<GameObjectType> typeList)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        if (BuffManager.Instance.Buffs.Select(b => b as BuffManager.ABuff)
            .Any(buff => buff?.Id == BuffId.Aggro && buff.Master.Id == gameObject.Id)) 
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
            if (dist < closestDist && targetable && obj.Id != gameObject.Id)
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }

    public GameObject? FindBuffTarget(GameObject gameObject, GameObjectType targetType)
    {
        Dictionary<int, GameObject> targetDict = AddTargetType(targetType);
        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        
        foreach (var (key, obj) in targetDict)
        {
            PositionInfo pos = obj.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = obj.Stat.Targetable;
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable && obj.Id != gameObject.Id)
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }

    public List<GameObject> FindBuffTargets(GameObject gameObject, GameObjectType targetType, int num)
    {
        Dictionary<int, GameObject> targetDict = AddTargetType(targetType);
        if (targetDict.Count == 0) return new List<GameObject>();
        
        List<GameObject> closestObjects = targetDict.Values
            .Where(obj => obj.Stat.Targetable)
            .Select(obj =>
            {
                PositionInfo pos = obj.PosInfo;
                Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
                float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
                return new { Object = obj, Distance = dist };
            })
            .OrderBy(item => item.Distance)
            .Take(num).Select(item => item.Object).ToList();

        return closestObjects;
    }
    
    public List<GameObject> FindBuffTargets(GameObject gameObject, GameObjectType targetType, float distance)
    {
        Dictionary<int, GameObject> targetDict = AddTargetType(targetType);
        if (targetDict.Count == 0) return new List<GameObject>();
        
        List<GameObject> closestObjects = targetDict.Values
            .Where(obj => obj.Stat.Targetable)
            .Where(obj => new Vector3().SqrMagnitude(obj.CellPos - gameObject.CellPos) < distance * distance)
            .ToList();

        return closestObjects;
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
    
    #endregion
    
    public bool ReachableInFence()
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