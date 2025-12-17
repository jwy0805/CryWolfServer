using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Data.SinglePlayScenario;
using Server.Game.Resources;

namespace Server.Game;

public partial class GameRoom
{
    private void GameInit()
    {
        Stopwatch.Start();
        _timeSendTime = Stopwatch.ElapsedMilliseconds;
        BaseInit();
    }
    
    private void BaseInit()
    {
        Console.WriteLine("Base Init");
        _portal = SpawnPortal();
        _storage = SpawnStorage();
        
        GameInfo = new GameInfo(_players, MapId)
        {
            FenceCenter = GameData.InitFenceCenter,
            FenceStartPos = GameData.InitFenceStartPos,
            FenceSize = GameData.InitFenceSize,
            SheepResource = GameMode is GameMode.Tutorial or GameMode.Test ? 2000 : 500,
            WolfResource = GameMode is GameMode.Tutorial or GameMode.Test ? 2000 : 500
        };

        SpawnFence(1, 1);
    }
    
    public void InfoInit(Player player)
    {
        if (_storage == null) return;
        
        // Set Monster Wave Module
        var stageFactory = new StageFactory();
        if (GameMode == GameMode.Tutorial)
        {
            var stageId = player.Faction == Faction.Sheep ? 1000 : 5000;
            _tutorialWaveModule = stageFactory.Create(stageId);
            _tutorialWaveModule.Room = this;
        }
        
        InitUiText();

        if (_players.Count == 2 && _infoInit == false)
        {
            SetAssets();
            _infoInit = true;
        }
    }

    private void InitUiText()
    {
        foreach (var player in _players.Values)
        {
            if (player.Session == null) continue;
            if (player.IsNpc) continue;            
            if (player.Faction == Faction.Sheep)
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxTower, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthTower, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.SheepResource});
                if (MapId != 1)
                {
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxTower, Max = true });
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthTower, Max = false });
                }
            }
            else
            {
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMaxMonster, Max = true });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = GameInfo.NorthMonster, Max = false });
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = GameInfo.WolfResource});
                if (MapId != 1)
                {
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMaxMonster, Max = true });
                    player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = GameInfo.SouthMonster, Max = false });
                }
            }
        }
    }
    
    public void HandlePlayerMove(Player? player, C_PlayerMove pMovePacket)
    {
        if (player == null) return;
        
        var playerMovePacket = new S_PlayerMove
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
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
                if (lackOfTowerCapacity)
                {
                    SendWarningMessage(player, "warning_in_game_capacity_limit_exceeded");
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
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
                if (lackOfMonsterCapacity)
                {
                    SendWarningMessage(player, "warning_in_game_capacity_limit_exceeded");
                    return;
                }
                SpawnMonsterStatue((UnitId)spawnPacket.Num, spawnPacket.PosInfo, player);
                GameInfo.NorthMonster++;
                break;
        }
    }

    public void HandleBindStatueInfo(Player? player, C_BindStatueInfo bindPacket)
    {
        if (FindGameObjectById(bindPacket.StatueId) is not MonsterStatue statue) return;
        
        var packet = new S_BindStatueInfo()
        {
            StatueId = statue.Id,
            UnitId = statue.UnitId,
        };
        
        Push(Broadcast, packet);
    }
    
    public void HandleState(Player? player, C_State statePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(statePacket.ObjectId);
        if (go == null) return;
        go.State = statePacket.State;
    }
    
    public void HandleEffectActivate(Player? player, C_EffectActivate dirPacket)
    {   
        // Effect 자체에 공격 등 효과가 있는 경우 Effect Controller에서 패킷 전송
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
            < 30 => ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarSilver),
            < 50 => ObjectManager.Instance.Create<Resource>(ResourceId.CoinStarGolden),
            < 100 => ObjectManager.Instance.Create<Resource>(ResourceId.PouchGreen),
            < 150 => ObjectManager.Instance.Create<Resource>(ResourceId.PouchRed),
            _ => ObjectManager.Instance.Create<Resource>(ResourceId.ChestGold)
        };

        resource.Yield = yield;
        resource.CellPos = gameObject.CellPos + new Vector3(0, 0.5f, 0);
        resource.Player = _players.FirstOrDefault(pair => pair.Value.Faction == Faction.Sheep).Value;
        resource.Init();
        Push(EnterGame, resource);
    }

    public void YieldDna(GameObject gameObject, GameObject attacker)
    {
        int yield = 0;
        switch (gameObject.ObjectType)
        {
            case GameObjectType.Tower:
                if (gameObject is Tower tower)
                {
                    if (DataManager.UnitDict.TryGetValue((int)tower.UnitId, out var unitData))
                    {
                        yield = unitData.Stat.RequiredResources;
                    }
                }
                break;
            case GameObjectType.Fence:
                if (_storage != null)
                {
                    yield = GameInfo.WolfYieldKillFence * (int)Math.Pow(2, _storage.Level - 1);
                }
                break;
            case GameObjectType.Sheep:
                yield = GameInfo.WolfYieldKillSheep;
                break;
        }

        if (attacker is Wolf { LastHitByWolf: true })
        {
            yield = (int)(yield * 1.1f); // Wolf가 공격한 경우 10% 추가
        }
        
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

    // Remain skills upgrade when the unit is upgraded.
    private void UpdateRemainSkills(Player player, UnitId unitId)
    {
        if (GameData.OwnSkills.TryGetValue(unitId, out var skills))
        {
            foreach (var skill in skills.Where(skill => player.SkillUpgradedList.Contains(skill) == false))
            {
                player.SkillSubject.SkillUpgraded(skill);
                player.SkillUpgradedList.Add(skill);
            }
        }
    }
}