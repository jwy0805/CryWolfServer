using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game.Resources;

namespace Server.Game;

public partial class GameRoom
{
    public int StorageLevel
    {
        get => _storageLevel;
        set
        {
            _storageLevel = value;
            if (_storageLevel > GameInfo.MaxStorageLevel)
            {
                _storageLevel = GameInfo.MaxStorageLevel;
                return;
            }
            GameInfo.StorageLevel = _storageLevel;

            switch (_storageLevel)
            {
                case 1:
                    GameInfo.NorthMaxTower = 8;
                    GameInfo.NorthMaxMonster = 8;
                    break;
                case 2:
                    GameInfo.NorthMaxTower = 12;
                    GameInfo.NorthMaxMonster = 12;
                    GameInfo.SheepYield += 20;
                    break;
                case 3: // 최대 레벨
                    GameInfo.NorthMaxTower = 16;
                    GameInfo.NorthMaxMonster = 16;
                    GameInfo.SheepYield += 40;
                    break;
            }
            
            // 울타리 생성
            if (_storageLevel != 1 && _fences.Count > 0)
            {   // 기존 울타리 삭제
                List<int> deleteFences = _fences.Keys.ToList();
                foreach (var fenceId in deleteFences) LeaveGame(fenceId);
                _fences.Clear();
            }
            
            SpawnFence(_storageLevel);
        }
    }
    
    private void GameInit()
    {
        Stopwatch.Start();
        _timeSendTime = Stopwatch.ElapsedMilliseconds;
        BaseInit();
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
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket) // 클라이언트의 요청으로 Spawn되는 경우
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;
        
        switch (type)
        {
            case GameObjectType.Tower:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                bool lackOfTowerCost = VerifyResourceForTowerSpawn(player, spawnPacket.Num);
                bool lackOfTowerCapacity = VerifyCapacityForTower(spawnPacket.Num, spawnPacket.Way);
                if (lackOfTowerCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
                if (lackOfTowerCapacity)
                {
                    SendWarningMessage(player, "인구수를 초과했습니다.");
                    return;
                }
                SpawnTower((UnitId)spawnPacket.Num, spawnPacket.PosInfo, player);
                GameInfo.NorthTower++;
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                SpawnMonster((UnitId)spawnPacket.Num, spawnPacket.PosInfo, player);
                break;
            
            case GameObjectType.MonsterStatue:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                bool lackOfMonsterCost = VerifyResourceForMonsterSpawn(player, spawnPacket.Num);
                bool lackOfMonsterCapacity = VerifyCapacityForMonster(spawnPacket.Num, spawnPacket.Way);
                if (lackOfMonsterCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
                if (lackOfMonsterCapacity)
                {
                    SendWarningMessage(player, "인구수를 초과했습니다.");
                    return;
                }
                SpawnMonsterStatue((UnitId)spawnPacket.Num, spawnPacket.PosInfo, player);
                GameInfo.NorthMonster++;
                break;
        }
    }

    public void HandleState(Player? player, C_State statePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(statePacket.ObjectId);
        if (go == null) return;
        go.State = statePacket.State;
    }
    
    public void HandleEffectActivate(Player? player, C_EffectActivate dirPacket)
    {   // Effect 자체에 공격 등 효과가 있는 경우 Effect Controller에서 패킷 전송
        if (player == null) return;
        GameObject? go = FindGameObjectById(dirPacket.ObjectId);
        if (go == null) return;
        var effect = (Effect)go;
        effect.PacketReceived = true;
    }
    
    public void YieldCoin(GameObject gameObject, int yield)
    {
        var resource = yield switch
        {
            < 50 => ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarSilver),
            < 100 => ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarGolden),
            < 200 => ObjectManager.Instance.Create<Resource>(ResourceId.PouchGreen),
            < 300 => ObjectManager.Instance.Create<Resource>(ResourceId.PouchRed),
            _ => ObjectManager.Instance.Create<Resource>(ResourceId.ChestGold)
        };

        resource.Yield = yield;
        resource.CellPos = gameObject.CellPos + new Vector3(0, 0.5f, 0);
        resource.Player = _players.FirstOrDefault(pair => pair.Value.Faction == Faction.Sheep).Value;
        resource.Init();
        Push(EnterGame, resource);
    }

    public void YieldDna(GameObject gameObject, int yield)
    {
        var resource = yield switch
        {
            < 50 => ObjectManager.Instance.Create<Resource>(ResourceId.Cell),
            < 100 => ObjectManager.Instance.Create<Resource>(ResourceId.MoleculeDouble),
            < 200 => ObjectManager.Instance.Create<Resource>(ResourceId.MoleculeTriple),
            < 300 => ObjectManager.Instance.Create<Resource>(ResourceId.MoleculeQuadruple),
            _ => ObjectManager.Instance.Create<Resource>(ResourceId.Dna)
        };
        
        resource.Yield = yield;
        resource.CellPos = gameObject.CellPos + new Vector3(0, 0.5f, 0);
        resource.Player = _players.FirstOrDefault(pair => pair.Value.Faction == Faction.Wolf).Value;
        resource.Init();
        Push(EnterGame, resource);
    }
    
    public void HandleChangeResource(Player? player, C_ChangeResource resourcePacket)
    {
        if (player == null) return;

        S_Despawn despawnPacket = new S_Despawn();
        int objectId = resourcePacket.ObjectId;
        despawnPacket.ObjectIds.Add(objectId);
        foreach (var p in _players.Values.Where(p => p.Id != objectId)) p.Session?.Send(despawnPacket);
        
        GameInfo.SheepResource += GameInfo.TotalSheepYield;
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}