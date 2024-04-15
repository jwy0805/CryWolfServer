using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.etc;

namespace Server.Game;

public partial class GameRoom 
{
    private void RegisterTower(Tower tower)
    {
        PositionInfo posInfo = new PositionInfo
        {
            PosX = tower.PosInfo.PosX,
            PosY = tower.PosInfo.PosY,
            PosZ = tower.PosInfo.PosZ,
            State = State.Idle
        };
        TowerSlot towerSlot = new(tower.UnitId, posInfo, tower.Way, tower.Id);
        
        if (towerSlot.Way == SpawnWay.North)
        {
            _northTowers.Add(towerSlot);
            GameInfo.NorthTower++;
        }
        else
        {
            _southTowers.Add(towerSlot);
            GameInfo.SouthTower++;
        }
        
        S_RegisterInSlot registerPacket = new()
        {
            ObjectId = towerSlot.ObjectId,
            UnitId = (int)towerSlot.TowerId,
            ObjectType = GameObjectType.Tower,
            Way = towerSlot.Way
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(registerPacket);
    }
    
    private void RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue.UnitId, statue.Way, statue);

        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
            GameInfo.NorthMonster++;
        }
        else
        {
            _southMonsters.Add(monsterSlot);
            GameInfo.SouthMonster++;
        }
        
        S_RegisterInSlot registerPacket = new()
        {
            ObjectId = monsterSlot.Statue.Id,
            UnitId = (int)monsterSlot.MonsterId,
            ObjectType = GameObjectType.MonsterStatue,
            Way = monsterSlot.Way
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)?.Session.Send(registerPacket);
    }

    private void UpgradeTower(Tower oldTower, Tower newTower)
    {
        TowerSlot newTowerSlot = new(newTower.UnitId, newTower.PosInfo, newTower.Way, newTower.Id);

        if (newTower.Way == SpawnWay.North)
        {
            int index = _northTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            if (index != -1)
            {
                _northTowers[index] = newTowerSlot;
            }
            else
            {
                int index2 = _southTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
                if (index2 == -1)
                {
                    var warningMsg = "(이미 죽었습니다)다시 시도해주세요.";
                    S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
                    _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(warningPacket);
                    return;
                }

                _southTowers[index2] = newTowerSlot;
            }
        }
        else
        {
            int index = _southTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            if (index != -1)
            {
                _southTowers[index] = newTowerSlot;
            }
            else
            {
                int index2 = _northTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
                if (index2 == -1)
                {
                    var warningMsg = "(이미 죽었습니다)다시 시도해주세요.";
                    S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
                    _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(warningPacket);
                    return;
                }
                _northTowers[index2] = newTowerSlot;
            }
        }
    }
    
    private void UpgradeMonsterStatue(MonsterStatue oldStatue, MonsterStatue newStatue)
    {
        MonsterSlot newMonsterSlot = new(newStatue.UnitId, newStatue.Way, newStatue);

        if (newStatue.Way == SpawnWay.North)
        {
            int index = _northMonsters.FindIndex(slot => slot.Statue.Id == oldStatue.Id);
            _northMonsters[index] = newMonsterSlot;
        }
        else
        {
            int index = _southMonsters.FindIndex(slot => slot.Statue.Id == oldStatue.Id);
            _southMonsters[index] = newMonsterSlot;
        }
    }
    
    private void SpawnFence(int storageLv = 1, int fenceLv = 0)
    {
        Vector3[] fencePos = GameData.GetPos(GameData.NorthFenceMax + GameData.SouthFenceMax, 8, GameData.FenceStartPos);
        float[] fenceRotation = GameData.GetRotation(GameData.NorthFenceMax + GameData.SouthFenceMax, 8);

        if (storageLv == 1)
        {
            for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
            {
                Fence fence = ObjectManager.Instance.Add<Fence>();
                fence.Init();
                fence.Info.Name = GameData.FenceName[storageLv];
                fence.CellPos = fencePos[i];
                fence.Dir = fenceRotation[i];
                fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
                fence.Room = this;
                fence.Way = fence.CellPos.Z > 0 ? SpawnWay.North : SpawnWay.South;
                if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
                else GameInfo.SouthFenceCnt++;
                fence.FenceNum = fenceLv;
                Push(EnterGame, fence);
            }
        }
        else if (storageLv == 2)
        {
            for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
            {
                Fence fence = ObjectManager.Instance.Add<Fence>();
                fence.Init();
                fence.Info.Name = GameData.FenceName[storageLv];
                fence.CellPos = fencePos[i];
                fence.Dir = fenceRotation[i] + 90;
                fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
                fence.Room = this;
                fence.Way = fence.CellPos.Z > 0 ? SpawnWay.North : SpawnWay.South;
                if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
                else GameInfo.SouthFenceCnt++;
                fence.FenceNum = fenceLv;
                Push(EnterGame, fence);
                
                EffectSetting(fence);
            }
        }
    }

    private void SpawnMonstersInNewRound()
    {
        var slots = _northMonsters.Concat(_southMonsters).ToList();
        
        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)!;
            var monster = EnterMonster((int)slot.MonsterId, FindMonsterSpawnPos(slot.Statue), player);
            monster.StatueId = slot.Statue.Id;
            Push(EnterGame, monster);
            player.Session.Send(new S_RegisterInSlot { ObjectId = monster.Id, UnitId = (int)monster.UnitId });
        }
    }

    private void SpawnTowersInNewRound()
    {
        // YieldCoin
        foreach (var sheep in _sheeps.Values)
        {
            if (sheep.YieldStop == false)
            {
                sheep.YieldCoin(GameInfo.SheepYield + sheep.YieldIncrement - sheep.YieldDecrement);
                sheep.YieldIncrement = 0;
                sheep.YieldDecrement = 0;
            }
            
            sheep.YieldStop = false;
        }
        
        List<TowerSlot> slots = _northTowers.Concat(_southTowers).ToList();

        // Spawn Towers
        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            // if (slot.ObjectId == 0) continue;
            var gameObject = FindGameObjectById(slot.ObjectId);
            if (gameObject == null || gameObject.Id == 0)
            {
                var tower = EnterTower((int)slot.TowerId, slot.PosInfo, player);
                RenewTowerSlot(slot, tower);
                Push(EnterGame, tower);
            }
            else
            {
                gameObject.Hp = gameObject.MaxHp;
                Broadcast(new S_ChangeHp { ObjectId = gameObject.Id, Hp = gameObject.Hp });
            }
        }
        
        // Reset State
        foreach (var tower in _towers.Values)
        {
            tower.State = State.Idle;
            Broadcast(new S_State { ObjectId = tower.Id, State = State.Idle });
        }
    }

    private Tower EnterTower(int unitId, PositionInfo posInfo, Player player)
    {
        var tower = ObjectManager.Instance.CreateTower((UnitId)unitId);
        tower.PosInfo = posInfo;
        tower.Info.PosInfo = tower.PosInfo;
        tower.Player = player;
        tower.UnitId = (UnitId)unitId;
        tower.Room = this;
        tower.Way = tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tower.Dir = tower.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tower.Init();
        return tower;
    }

    private Monster EnterMonster(int unitId, PositionInfo posInfo, Player player)
    {
        var monster = ObjectManager.Instance.CreateMonster((UnitId)unitId);
        monster.PosInfo = posInfo;
        monster.Info.PosInfo = monster.PosInfo;
        monster.Player = player;
        monster.UnitId = (UnitId)unitId;
        monster.Room = this;
        monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        monster.Dir = monster.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        monster.Init();
        return monster;
    }

    private MonsterStatue EnterMonsterStatue(int unitId, PositionInfo posInfo, Player player)
    {
        var statue = ObjectManager.Instance.CreateMonsterStatue();
        statue.PosInfo = posInfo;
        statue.Info.PosInfo = statue.PosInfo;
        statue.Player = player;
        statue.UnitId = (UnitId)unitId;
        statue.Room = this;
        statue.Way = statue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        statue.Dir = statue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        statue.Init();
        return statue;
    }

    private Sheep EnterSheep(Player player)
    {
        Sheep sheep = ObjectManager.Instance.Add<Sheep>();
        sheep.PosInfo = new PositionInfo { State = State.Idle };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Room = this;
        sheep.Player = player;
        sheep.Init();
        sheep.CellPos = Map.FindSpawnPos(sheep);

        return sheep;
    }

    private void EnterSheepByServer(Player player)
    {
        var sheep = ObjectManager.Instance.Add<Sheep>();
        sheep.PosInfo = new PositionInfo { State = State.Idle };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Room = this;
        sheep.Player = player;
        sheep.Init();
        sheep.CellPos = Map.FindSpawnPos(sheep);
        EnterGame(sheep);
        GameInfo.SheepCount++;
    }
    
    private Tusk EnterTusk(int unitId, PositionInfo posInfo, Player player)
    {
        var tusk = ObjectManager.Instance.CreateTusk();
        tusk.PosInfo = posInfo;
        tusk.Info.PosInfo = tusk.PosInfo;
        tusk.Player = player;
        tusk.UnitId = (UnitId)unitId;
        tusk.Room = this;
        tusk.Way = tusk.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tusk.Dir = tusk.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tusk.Init();
        return tusk;
    }
}