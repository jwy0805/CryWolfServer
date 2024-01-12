using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.etc;

namespace Server.Game;

public partial class GameRoom 
{
    private void RegisterTower(Tower tower) 
    {
        TowerSlot towerSlot = new(tower.TowerId, tower.Way, tower.Id);
        int slotNum = 0;
        if (towerSlot.Way == SpawnWay.North)
        {
            _northTowers.Add(towerSlot);
            slotNum = _northTowers.Count - 1;
        }
        else
        {
            _southTowers.Add(towerSlot);
            slotNum = _southTowers.Count - 1;
        }
        
        S_RegisterTower registerPacket = new()
        {
            TowerId = (int)towerSlot.TowerId,
            ObjectId = towerSlot.ObjectId,
            Way = towerSlot.Way,
            SlotNumber = slotNum
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(registerPacket);
    }

    private void RegisterMonster(Monster monster)
    {
        
    }

    private void RegisterTowerStatue(TowerStatue towerStatue)
    {
        
    }
    
    private void RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue.MonsterId, statue.Way, 0, statue);
        int slotNum = 0;
        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
            slotNum = _northMonsters.Count - 1;
        }
        else
        {
            _southMonsters.Add(monsterSlot);
            slotNum = _southMonsters.Count - 1;
        }
        
        S_RegisterMonster registerPacket = new()
        {
            MonsterId = (int)monsterSlot.MonsterId,
            ObjectId = monsterSlot.Statue.Id,
            Way = monsterSlot.Way,
            SlotNumber = slotNum
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)?.Session.Send(registerPacket);
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
        List<MonsterSlot> slots = _northMonsters.Concat(_southMonsters)
            .Where(slot =>
            {
                DataManager.TowerDict.TryGetValue((int)slot.MonsterId, out var towerData);
                var behavior = (Behavior)Enum.Parse(typeof(Behavior), towerData!.behavior);
                return behavior == Behavior.Offence;            
            }).ToList();
        
        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)!;
            if (slot.Statue == null) continue;
            var monster = EnterMonster((int)slot.MonsterId, FindMonsterSpawnPos(slot.Statue), player);
            Push(EnterGame, monster);
        }
    }

    private void SpawnTowersInNewRound()
    {
        List<TowerSlot> slots = _northTowers.Concat(_southTowers)
            .Where(slot => 
            {
                DataManager.TowerDict.TryGetValue((int)slot.TowerId, out var towerData);
                var behavior = (Behavior)Enum.Parse(typeof(Behavior), towerData!.behavior);
                return behavior == Behavior.Offence;
            }).ToList();

        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            if (slot.Statue == null) continue;
            var tower = EnterTower((int)slot.TowerId, FindTowerSpawnPos(slot.Statue), player);
            Push(EnterGame, tower);
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
}