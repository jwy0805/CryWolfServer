using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    public void SpawnStatueForTest(UnitId monsterId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        SpawnMonsterStatue(monsterId, pos, npc);
    }
    
    public MonsterStatue SpawnStatue(UnitId monsterId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var statue = SpawnMonsterStatue(monsterId, pos, npc);
        SpawnEffect(EffectId.Upgrade, statue, statue);
        
        return statue;
    }    

    public void SpawnTower(UnitId towerId, PositionInfo pos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var tower = SpawnTower(towerId, pos, npc);
        SpawnEffect(EffectId.Upgrade, tower, tower);
    }

    public Tower SpawnTowerOnRelativeZ(UnitId unitId, Vector3 relativePos)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        var pos = new PositionInfo
        {
            PosX = relativePos.X,
            PosY = relativePos.Y,
            PosZ = GameInfo.FenceStartPos.Z + relativePos.Z,
        };
        var tower = SpawnTower(unitId, pos, npc);
        SpawnEffect(EffectId.Upgrade, tower, tower);

        return tower;
    }  
    
    public void UpgradeSkill(Skill skill)
    {
        var npc = FindPlayer(go => go is Player { IsNpc: true}); 
        if (npc == null) return;
        npc.SkillSubject.SkillUpgraded(skill);
        npc.SkillUpgradedList.Add(skill);
    }

    public void UpgradeBaseSkill(Skill skill, Player player)
    {
        if (_storage == null || _portal == null) return;
        
        switch (skill)
        {
            case Skill.RepairSheep:
                RepairFences(_fences.Values.ToList());
                break;
            case Skill.RepairWolf:
                RepairStatues(_statues.Values.ToList());
                break;
            case Skill.BaseUpgradeSheep:
                _storage.LevelUp();
                break;
            case Skill.BaseUpgradeWolf:
                _portal.LevelUp();
                break;
            case Skill.ResourceSheep:
                break;
            case Skill.ResourceWolf:
                break;
            case Skill.AssetSheep:
                SpawnSheep(player);
                break;
            case Skill.AssetWolf:
                if (Enchant is not { EnchantLevel: < 5 }) return;
                Enchant.EnchantLevel++;
                break;
        }
    }

    public void UpgradeTowers(UnitId unitId, Player player)
    {
        var towers = _towers.Values
            .Where(tower => tower.UnitId == unitId && tower.Player == player).ToList();
        foreach (var tower in towers)
        {
            UpgradeUnit(tower, player);
        }
        
        UpdateCurrentUnits(player, unitId);
    }

    public void UpgradeStatues(UnitId unitId, Player player)
    {
        var statues = _statues.Values
            .Where(statue => statue.UnitId == unitId && statue.Player == player).ToList();
        foreach (var statue in statues)
        {
            UpgradeUnit(statue, player);
        }
        
        UpdateCurrentUnits(player, unitId);
    }
    
    private void UpgradeUnit(GameObject gameObject, Player player)
    {
        var id = gameObject.Id;
        PositionInfo newPos = new()
        {
            PosX = gameObject.PosInfo.PosX,
            PosY = gameObject.PosInfo.PosY,
            PosZ = gameObject.PosInfo.PosZ,
        };

        GameObject newObject;
        if (gameObject.ObjectType == GameObjectType.Tower)
        {
            if (gameObject is not Tower tower) return;
            var hpPortion = tower.Hp / (float)tower.MaxHp;
            LeaveGame(id);
            newPos.State = gameObject.State != State.Die ? State.Idle : State.Die;
            newObject = SpawnTower(tower.UnitId + 1, newPos, player);
            newObject.Hp = (int)(newObject.MaxHp * hpPortion);
            
            if (newPos.State == State.Die)
            {
                newObject.Targetable = false;
                DieAndLeave(newObject.Id);
            }
            
            newObject.BroadcastState();
            
            var soundPacket = new S_PlaySound
                { ObjectId = newObject.Id, Sound = Sounds.UpgradeTower, SoundType = SoundType.D3 };
            Push(Broadcast, soundPacket);
        }
        else if (gameObject.ObjectType == GameObjectType.MonsterStatue)
        {
            if (gameObject is not MonsterStatue statue) return;
            LeaveGame(id);
            newObject = SpawnMonsterStatue(statue.UnitId + 1, newPos, player);
            UpdateCurrentUnits(player, statue.UnitId);

            var soundPacket = new S_PlaySound
                { ObjectId = newObject.Id, Sound = Sounds.UpgradeStatue, SoundType = SoundType.D3 };
            Push(Broadcast, soundPacket);
        }
        else
        {
            return;
        }
        
        SpawnEffect(EffectId.Upgrade, newObject, newObject);
    }

    private void UpdateCurrentUnits(Player player, UnitId unitId)
    {
        Console.Write("Updating current unit: ");
        for (int i = 0; i < player.CurrentUnitIds.Length; i++)
        {
            Console.Write($"{player.CurrentUnitIds[i]} ");
            if (player.CurrentUnitIds[i] == unitId)
            {
                player.CurrentUnitIds[i] = unitId + 1;
                break; 
            }
        }
        Console.WriteLine();

        Console.Write("Updated: ");
        foreach (var id in player.CurrentUnitIds)
        {
            Console.Write($"{id} ");
        }
        Console.WriteLine();
    }

    // Only for single play scenario
    public void UpgradeUnitSingle(Dictionary<string, Tower> towers, string key, Player player)
    {
        if (towers.TryGetValue(key, out var tower))
        {
            var id = tower.Id;
            PositionInfo newPos = new()
            {
                PosX = tower.PosInfo.PosX,
                PosY = tower.PosInfo.PosY,
                PosZ = tower.PosInfo.PosZ,
            };
            
            LeaveGame(id);
            var newObject = SpawnTower(tower.UnitId + 1, newPos, player);
            towers[key] = newObject;
        }
    }

    private void TestCaseSheep0(int round)
    {
        switch (round)
        {
            case 0:
                // SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -5, PosY = 6, PosZ = 12 });
                // SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -3, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -1, PosY = 6, PosZ = 12 });
                break;
        }
    }
    
    private void TestCaseSheep1(int round)
    {
        switch (round)
        {
            case 0:
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -5, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -3, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = -1, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = 1, PosY = 6, PosZ = 12 });
                SpawnStatue(UnitId.Wolf, new PositionInfo { PosX = 3, PosY = 6, PosZ = 12 });
                break;
            default:
                
                break;
        }
    }

    private void TestCaseSheep4(int round)
    {
        switch (round)
        {
            case 0:
                var pos1 = new PositionInfo { PosX = -1.5f, PosY = 6, PosZ = 13 };
                var pos2 = new PositionInfo { PosX = 1.5f, PosY = 6, PosZ = 13 };
                var pos3 = new PositionInfo { PosX = 0, PosY = 6, PosZ = 14 };
                var pos4 = new PositionInfo { PosX = -1, PosY = 6, PosZ = 16 };
                var pos5 = new PositionInfo { PosX = 1, PosY = 6, PosZ = 16 };
                SpawnStatue(UnitId.Cacti, pos1);
                SpawnStatue(UnitId.Cacti, pos2);
                SpawnStatue(UnitId.WolfPup, pos3);
                SpawnStatue(UnitId.Snakelet, pos4);
                SpawnStatue(UnitId.Snakelet, pos5);
                break;
            case 1:
                break;
            case 2:
                break;
        }
    }
    
    private void TestCaseWolf0(int round)
    {
        var fencePosZ = GameInfo.FenceStartPos.Z;
        switch (round)
        {
            case 0:
                PositionInfo pos1 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ + 2 };
                SpawnTower(UnitId.TargetDummy, pos1);
                break;
        }
    }

    private void TestCaseWolf1(int round)
    {
        var fencePosZ = GameInfo.FenceStartPos.Z;
        switch (round)
        {
            case 0:
                PositionInfo pos1 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos2 = new() { PosX = 4, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos3 = new() { PosX = -4, PosY = 6, PosZ = fencePosZ + 2 };
                PositionInfo pos4 = new() { PosX = 0, PosY = 6, PosZ = fencePosZ -1 };
                SpawnTower(UnitId.TrainingDummy, pos1);
                SpawnTower(UnitId.TrainingDummy, pos2);
                SpawnTower(UnitId.TrainingDummy, pos3);
                SpawnTower(UnitId.Bloom, pos4);
                break;
        }
    }
}