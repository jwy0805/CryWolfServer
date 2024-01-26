using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.etc;

namespace Server.Game;

public partial class GameRoom 
{
    private void RegisterTower(Tower tower) 
    {
        TowerSlot towerSlot = new(tower.TowerId, tower.Way, tower.Id);
        
        if (towerSlot.Way == SpawnWay.North)
        {
            _northTowers.Add(towerSlot);
        }
        else
        {
            _southTowers.Add(towerSlot);
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
        MonsterSlot monsterSlot = new(statue.MonsterId, statue.Way, statue);

        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
        }
        else
        {
            _southMonsters.Add(monsterSlot);
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
        TowerSlot newTowerSlot = new(newTower.TowerId, newTower.Way, newTower.Id);

        if (newTower.Way == SpawnWay.North)
        {
            int index = _northTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            _northTowers[index] = newTowerSlot;
        }
        else
        {
            int index = _southTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            _southTowers[index] = newTowerSlot;
        }
    }
    
    private void UpgradeMonsterStatue(MonsterStatue oldStatue, MonsterStatue newStatue)
    {
        MonsterSlot newMonsterSlot = new(newStatue.MonsterId, newStatue.Way, newStatue);

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

        for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
        {
            Fence fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceName[storageLv];
            fence.CellPos = fencePos[i];
            fence.Dir = fenceRotation[i];
            fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            fence.Room = this;
            fence.FenceNum = fenceLv;
            Push(EnterGame, fence);
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
            player.Session.Send(new S_RegisterInSlot { ObjectId = monster.Id, UnitId = (int)monster.MonsterId });
        }
    }

    private void SpawnTowersInNewRound()
    {
        List<TowerSlot> slots = _northTowers.Concat(_southTowers).ToList();

        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            if (slot.ObjectId == 0) continue;
            var gameObject = FindGameObjectById(slot.ObjectId);
            if (gameObject == null)
            {
                // Create New Tower
            }
            else
            {
                gameObject.Hp = gameObject.MaxHp;
            }
        }
    }

    private Tower EnterTower(int towerId, PositionInfo posInfo, Player player)
    {
        var tower = ObjectManager.Instance.CreateTower((TowerId)towerId);
        tower.PosInfo = posInfo;
        tower.Info.PosInfo = tower.PosInfo;
        tower.TowerNum = towerId;
        tower.Player = player;
        tower.TowerId = (TowerId)towerId;
        tower.Room = this;
        tower.Way = tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tower.Dir = tower.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tower.Init();
        return tower;
    }

    private Monster EnterMonster(int monsterId, PositionInfo posInfo, Player player)
    {
        var monster = ObjectManager.Instance.CreateMonster((MonsterId)monsterId);
        monster.PosInfo = posInfo;
        monster.Info.PosInfo = monster.PosInfo;
        monster.MonsterNum = monsterId;
        monster.Player = player;
        monster.MonsterId = (MonsterId)monsterId;
        monster.Room = this;
        monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        monster.Dir = monster.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        monster.Init();
        return monster;
    }

    private MonsterStatue EnterMonsterStatue(int monsterId, PositionInfo posInfo, Player player)
    {
        var statue = ObjectManager.Instance.CreateMonsterStatue();
        statue.PosInfo = posInfo;
        statue.Info.PosInfo = statue.PosInfo;
        statue.MonsterNum = monsterId;
        statue.Player = player;
        statue.MonsterId = (MonsterId)monsterId;
        statue.Room = this;
        statue.Way = statue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        statue.Dir = statue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        statue.Init();
        return statue;
    }
    
    private Tusk EnterTusk(int monsterId, PositionInfo posInfo, Player player)
    {
        var tusk = ObjectManager.Instance.CreateTusk();
        tusk.PosInfo = posInfo;
        tusk.Info.PosInfo = tusk.PosInfo;
        tusk.MonsterNum = monsterId;
        tusk.Player = player;
        tusk.MonsterId = (MonsterId)monsterId;
        tusk.Room = this;
        tusk.Way = tusk.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tusk.Dir = tusk.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tusk.Init();
        return tusk;
    }
}