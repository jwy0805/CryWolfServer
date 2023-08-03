using System.Diagnostics;
using System.Numerics;
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

        int id = movePacket.ObjectId;
        GameObject? go = FindGameObjectById(id);
        Vector3 cellPos = new Vector3(movePacket.PosX, movePacket.PosY, movePacket.PosZ);
        go?.ApplyMap(cellPos);
    }

    public void HandleSetDest(Player? player, C_SetDest destPacket)
    {
        if (player == null) return;

        int id = destPacket.ObjectId;
        GameObject? go = FindGameObjectById(id);
        go?.BroadcastDest();
    }
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket)
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;

        switch (type)
        {
            case GameObjectType.Tower:
                Tower tower = ObjectManager.Instance.Add<Tower>();
                tower.PosInfo = spawnPacket.PosInfo;
                tower.TowerNo = spawnPacket.Num;
                Push(EnterGame, tower);
                break;
            case GameObjectType.Monster:
                Monster monster = ObjectManager.Instance.Add<Monster>();
                monster.Init();
                monster.PosInfo = spawnPacket.PosInfo;
                monster.CellPos = Map.FindSpawnPos(monster, spawnPacket.Way);
                monster.Info.PosInfo = monster.PosInfo;
                monster.MonsterNo = spawnPacket.Num;
                Push(EnterGame, monster);
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

    public void HandleAttack(Player? player, C_Attack attackPacket)
    {
        if (player == null) return;
        int attackerId = attackPacket.ObjectId;
        GameObject? attacker = FindGameObjectById(attackerId);
        if (attacker == null) return;

        int damage = attacker.TotalAttack;
        GameObject? target = attacker.Target;
        target?.OnDamaged(attacker, damage);
        SetNextState(attacker);
    }
    
    public GameObject? FindTarget(GameObject gameObject)
    {
        // 어그로 끌린 상태면 리턴하는 코드
        //

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

        if (targetDict.Count == 0) return null;
        GameObject? target = null;
        
        float closestDist = 5000f;
        foreach (var (key, obj) in targetDict)
        {
            PositionInfo pos = obj.PosInfo;
            Vector3 targetPos = new Vector3(pos.PosX, pos.PosY, pos.PosZ);
            bool targetable = obj.Stat.Targetable; 
            float dist = new Vector3().SqrMagnitude(targetPos - gameObject.CellPos);
            if (dist < closestDist && targetable)
            {
                closestDist = dist;
                target = obj;
            }
        }

        return target;
    }

    public GameObject? FindGameObjectById(int id)
    {
        GameObject? go = new GameObject();
        GameObjectType type = ObjectManager.GetObjectTypeById(id);
        switch (type)
        {
            case GameObjectType.Tower:
                if (_towers.ContainsKey(id)) go = _towers[id];
                break;
            case GameObjectType.Sheep:
                if (_sheeps.ContainsKey(id)) go = _sheeps[id];
                break;
            case GameObjectType.Monster:
                if (_monsters.ContainsKey(id)) go = _monsters[id];
                break;
            case GameObjectType.Projectile:
                break;
            default:
                go = null;
                break;
        }

        return go;
    }

    private void SetNextState(GameObject attacker)
    {
        GameObject? target = attacker.Target;
        State state;
        if (target == null || target.Stat.Targetable == false)
        {
            state = State.Idle;
        }
        else
        {
            if (target.Hp > 0)
            {
                Vector3 targetPos = attacker.Room!.Map.GetClosestPoint(attacker.CellPos, target);
                float distance = (float)Math.Sqrt(new Vector3().SqrMagnitude(targetPos - attacker.CellPos));
                state = distance <= attacker.Stat.AttackRange ? State.Attack : State.Moving;
            }
            else
            {
                attacker.Target = null;
                state = State.Idle;
            }
        }

        if (target == null) return;
        S_ChangeState statePacket = new S_ChangeState { ObjectId = target.Id, State = state };
        Broadcast(statePacket);
    }
    
    public bool ReachableInFence()
    {
        return _fences.Count < GameData.FenceCnt[_storageLevel];
    }
}