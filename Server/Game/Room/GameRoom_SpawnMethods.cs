using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.etc;

namespace Server.Game;

public partial class GameRoom 
{
    private void RegisterTower(Tower tower) 
    {
        TowerSlot towerSlot = new(tower.Id, tower.TowerId, tower.Way);
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
        MonsterSlot monsterSlot = new(statue, statue.MonsterId, statue.Way);
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
        List<MonsterSlot> slots = _northMonsters.Concat(_southMonsters).ToList();
        foreach (var slot in slots)
        {
            Monster monster = ObjectManager.Instance.CreateMonster(slot.MonsterId);
            monster.MonsterNum = slot.Statue.MonsterNum;
            monster.PosInfo = FindMonsterSpawnPos(slot.Statue);
            monster.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)!;
            monster.MonsterId = slot.MonsterId;
            monster.Way = slot.Way;
            monster.Room = this;
            monster.Init();
            monster.CellPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
            Push(EnterGame, monster);
        }
    }

    private void SpawnTowersInNewRound()
    {
        
    }

    private void SetTowerInfo()
    {
        
    }

    private void SetMonsterInfo()
    {
        
    }
}