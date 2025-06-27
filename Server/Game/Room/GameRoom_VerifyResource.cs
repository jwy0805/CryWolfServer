using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    /*
     * Verify => Check if the player has enough resources to perform the action
     * Calc => Calculate the cost of the action and subtract it from the player's resources
     */
    private bool VerifyResourceForTowerSpawn(Player player, int unitId)
    {
        if (!DataManager.UnitDict.TryGetValue(unitId, out var towerData)) return true;
        int resource = GameInfo.SheepResource;
        int cost = towerData.stat.RequiredResources;
        if (resource < cost) return true;
        GameInfo.SheepResource -= cost;
        
        return false;
    }
    
    private bool VerifyResourceForMonsterSpawn(Player player, int unitId)
    {
        int resource = GameInfo.WolfResource;
        if (!DataManager.UnitDict.TryGetValue(unitId, out var monsterData)) return true;
        int cost = monsterData.stat.RequiredResources;
        if (resource < cost) return true;
        GameInfo.WolfResource -= cost;
        
        return false;
    }

    private bool VerifyUpgradeTowerPortrait(Player player, UnitId unitId)
    {
        int resource = GameInfo.SheepResource;
        int cost = CalcUpgradePortrait(player, unitId);
        if (resource < cost) return true;
        GameInfo.SheepResource -= cost;

        return false;
    }
    
    private bool VerifyUpgradeMonsterPortrait(Player player, UnitId unitId)
    {
        int resource = GameInfo.WolfResource;
        int cost = CalcUpgradePortrait(player, unitId);
        if (resource < cost) return true;
        GameInfo.WolfResource -= cost;

        return false;
    }
    
    private bool VerifyUnitUpgrade(Player player, int unitId)
    {
        if (player.Portraits.Contains(unitId + 1)) return false;
        return true;
    }

    private bool VerifyUnitUpgradeCost(int unitId)
    {
        var cost = CalcUpgradeCost(unitId);

        if (cost > GameInfo.SheepResource) return true;
        GameInfo.SheepResource -= cost;
        
        return false;
    }
    
    private bool VerityStatueUpgradeCost(int unitId)
    {
        var cost = CalcUpgradeCost(unitId);

        if (cost > GameInfo.WolfResource) return true;
        GameInfo.WolfResource -= cost;
        
        return false;
    }
    
    private bool VerifyCapacityForTower(int towerId, SpawnWay way)
    {
        if (!DataManager.UnitDict.TryGetValue(towerId, out _)) return true;
        int maxCapacity = 0;
        int nowCapacity = 0;
        
        if (way == SpawnWay.North)
        {
            maxCapacity = GameInfo.NorthMaxTower;
            nowCapacity = GameInfo.NorthTower;
        }
        else if (way == SpawnWay.South)
        {
            maxCapacity = GameInfo.SouthMaxTower;
            nowCapacity = GameInfo.SouthTower;
        }

        return nowCapacity >= maxCapacity;
    }
    
    private bool VerifyCapacityForMonster(int monsterId, SpawnWay way)
    {
        if (!DataManager.UnitDict.TryGetValue(monsterId, out _)) return true;
        int maxCapacity = 0;
        int nowCapacity = 0;
        
        if (way == SpawnWay.North)
        {
            maxCapacity = GameInfo.NorthMaxMonster;
            nowCapacity = GameInfo.NorthMonster;
        }
        else if (way == SpawnWay.South)
        {
            maxCapacity = GameInfo.SouthMaxMonster;
            nowCapacity = GameInfo.SouthMonster;
        }

        return nowCapacity >= maxCapacity;
    }
    
    private bool VerifyResourceForSkillUpgrade(Player player, Skill skill)
    {
        if (!DataManager.SkillDict.TryGetValue((int)skill, out var skillData)) return true;
        var cost = skillData.cost;
        var resource = player.Faction == Faction.Sheep ? GameInfo.SheepResource : GameInfo.WolfResource;
        if (resource < cost) return true;
        return false;
    }

    private bool VerifySkillTree(Player player, Skill skill)
    {
        var skills = GameData.SkillTree[skill];
        if (skills.Contains(Skill.NoSkill)) return false;
        return !skills.All(item => player.SkillUpgradedList.Contains(item));
    }

    private int CalcUpgradePortrait(Player player, UnitId unitId)
    {
        GameData.OwnSkills.TryGetValue(unitId, out var skills);
        if (skills == null) return 100000;
        int cost = 0;
        foreach (var skill in skills)
        {
            if (player.SkillUpgradedList.Contains(skill)) continue;
            if (!DataManager.SkillDict.TryGetValue((int)skill, out var skillData)) continue;
            cost += skillData.cost;
        }

        if (DataManager.UnitDict.TryGetValue((int)unitId, out var unitData) 
            && DataManager.UnitDict.TryGetValue((int)unitId + 1, out var newUnitData))
        {
            var upgradeCost = (int)((newUnitData.stat.RequiredResources - unitData.stat.RequiredResources) * 0.5f);
            return cost + upgradeCost;
        }
        
        return 0;
    }
    
    private int CalcFenceRepairCost(int[] objectIds, bool all = false)
    {
        var fenceDict = all ? _fences : _fences
            .Where(kv => objectIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var damaged = fenceDict.Sum(fence => fence.Value.MaxHp - fence.Value.Hp);
        var cost = (int)(damaged * 0.4);
        return cost;
    }

    private int CalcStatueRepairCost(int[] objectIds, bool all = false)
    {
        var statueDict = all ? _statues : _statues
            .Where(kv => objectIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var damaged = statueDict.Sum(statue => statue.Value.MaxHp - statue.Value.Hp);
        var cost = (int)(damaged * 0.4);
        return cost;
    }
    
    private int CalcUnitUpgradeCost(int[] objectIds)
    {
        var cost = 0;
        foreach (var objectId in objectIds)
        {
            var gameObject = FindGameObjectById(objectId);
            var unitId = gameObject switch
            {
                MonsterStatue statue => (int)statue.UnitId,
                Tower tower => (int)tower.UnitId,
                _ => 0
            };
            cost += CalcUpgradeCost(unitId);
        }
        
        return cost;
    }
    
    private int CalcUnitDeleteCost(int[] objectIds)
    {
        var cost = 0;
        foreach (var objectId in objectIds)
        {
            var gameObject = FindGameObjectById(objectId);
            var unitId = gameObject switch
            {
                MonsterStatue statue => (int)statue.UnitId,
                Tower tower => (int)tower.UnitId,
                _ => 0
            };
            cost += CalcUpgradeCost(unitId);
        }
        
        return (int)(cost * 0.5);
    }
    
    private int CalcUpgradeCost(int unitId)
    {
        // Unit Level is 3
        if (unitId % 100 % 3 == 0) return 0;
        
        var oldCost = 0;
        var newCost = 0;
        if (DataManager.UnitDict.TryGetValue(unitId, out var oldData)) oldCost = oldData.stat.RequiredResources;
        if (DataManager.UnitDict.TryGetValue(unitId + 1, out var newData)) newCost = newData.stat.RequiredResources;
        return newCost - oldCost;
    }

    private int CheckBaseSkillCost(Skill skill)
    {
        int cost = 0;
        switch (skill)
        {
            case Skill.RepairSheep:
                cost = CalcFenceRepairCost(Array.Empty<int>());
                break;
            case Skill.RepairWolf:
                cost = CalcStatueRepairCost(Array.Empty<int>());
                break;
            case Skill.BaseUpgradeSheep:
                if (_storage != null)
                {
                    cost = GameInfo.StorageLevelUpCost * _storage.Level;
                }
                break;
            case Skill.BaseUpgradeWolf:
                if (_portal != null)
                {
                    cost = GameInfo.StorageLevelUpCost * _portal.Level;
                }
                break;
            case Skill.ResourceSheep:
                cost = GameInfo.SheepYieldUpgradeCost;
                break;
            case Skill.ResourceWolf:
                cost = GameInfo.WolfYield * 3;
                break;
            case Skill.AssetSheep:
                cost = GameInfo.SheepCount * 150;
                break;
            case Skill.AssetWolf:
                cost = Enchant == null ? 0 : GameInfo.EnchantUpCost * (Enchant.EnchantLevel + 1);
                break;
        }
        
        return cost;
    }
    
    private void RepairFences(List<Fence> fences)
    {
        foreach (var fence in fences)
        {
            fence.Hp = fence.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = fence.Id, Hp = fence.Hp });
        }
    }
    
    private void RepairAllFences()
    {
        foreach (var fence in _fences.Values)
        {
            fence.Hp = fence.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = fence.Id, Hp = fence.Hp });
        }
    }
    
    private void RepairStatues(List<MonsterStatue> statues)
    {
        foreach (var statue in statues)
        {
            statue.Hp = statue.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = statue.Id, Hp = statue.Hp });
        }
    }
    
    private void SendWarningMessage(Player player, string msg)
    {
        S_SendWarningInGame warningPacket = new() { MessageKey = msg };
        player.Session?.Send(warningPacket);
    }
}