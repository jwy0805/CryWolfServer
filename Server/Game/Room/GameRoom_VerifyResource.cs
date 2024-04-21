using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private bool VerifyResourceForTower(Player player, int unitId)
    {
        if (!DataManager.UnitDict.TryGetValue(unitId, out var towerData)) return true;
        int resource = GameInfo.SheepResource;
        int cost = towerData.stat.RequiredResources;
        if (resource < cost) return true;
        GameInfo.SheepResource -= cost;
        
        return false;
    }
    
    private bool VerifyResourceForMonster(Player player, int unitId)
    {
        int resource = GameInfo.WolfResource;
        if (!DataManager.UnitDict.TryGetValue(unitId, out var monsterData)) return true;
        int cost = monsterData.stat.RequiredResources;
        if (resource < cost) return true;
        GameInfo.WolfResource -= cost;
        
        return false;
    }

    private bool CalcUpgradeTowerPortrait(Player player, UnitId unitId)
    {
        int resource = GameInfo.SheepResource;
        int cost = VerifyUpgradePortrait(player, unitId);
        if (resource < cost) return true;
        GameInfo.SheepResource -= cost;

        return false;
    }
    
    private bool CalcUpgradeMonsterPortrait(Player player, UnitId unitId)
    {
        int resource = GameInfo.WolfResource;
        int cost = VerifyUpgradePortrait(player, unitId);
        if (resource < cost) return true;
        GameInfo.WolfResource -= cost;

        return false;
    }

    private int VerifyUpgradePortrait(Player player, UnitId unitId)
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
        
        return cost;
    }
    
    private bool VerifyCapacityForTower(Player player, int towerId, SpawnWay way)
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
    
    private bool VerifyCapacityForMonster(Player player, int monsterId, SpawnWay way)
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
    
    private bool VerifyResourceForSkill(Player player, Skill skill)
    {
        if (!DataManager.SkillDict.TryGetValue((int)skill, out var skillData)) return true;
        var cost = skillData.cost;
        var resource = player.Camp == Camp.Sheep ? GameInfo.SheepResource : GameInfo.WolfResource;
        if (resource < cost) return true;
        if (player.Camp == Camp.Sheep) GameInfo.SheepResource -= cost;
        else GameInfo.WolfResource -= cost;
        return false;
    }

    private bool VerifySkillTree(Player player, Skill skill)
    {
        var skills = GameData.SkillTree[skill];
        if (skills.Contains(Skill.NoSkill)) return false;
        return !skills.All(item => player.SkillUpgradedList.Contains(item));
    }

    private int CheckBaseSkillCost(Skill skill)
    {
        int cost = 0;
        switch (skill)
        {
            case Skill.FenceRepair:
                cost = VerifyFenceRepairCost();
                break;
            case Skill.StorageLvUp:
                cost = GameInfo.StorageLevelUpCost;
                break;
            case Skill.GoldIncrease:
                cost = GameInfo.SheepYield * 3;
                break;
            case Skill.SheepHealth:
                DataManager.ObjectDict.TryGetValue(1, out var sheepData);
                cost = (int)(sheepData!.stat.MaxHp * 2.5);
                break;
            case Skill.SheepIncrease:
                cost = (int)(GameInfo.SheepCount * 50 * (1 + GameInfo.SheepCount * 0.25));
                break;
            default:
                break;
        }
        
        return cost;
    }
    
    private int VerifyFenceRepairCost()
    {
        int damaged = _fences.Sum(fence => fence.Value.MaxHp - fence.Value.Hp);
        int cost = (int)(damaged * 0.4);
        return cost;
    }

    private void FenceRepair()
    {
        foreach (var fence in _fences)
        {
            fence.Value.Hp = fence.Value.MaxHp;
            Broadcast(new S_ChangeHp { ObjectId = fence.Key, Hp = fence.Value.Hp });
        }
    }

    private bool VerifyUnitUpgrade(Player player, int unitId)
    {
        if (player.Portraits.Contains(unitId + 1)) return false;
        return true;
    }

    private bool VerifyUnitUpgradeCost(int unitId)
    {
        int cost = 0;
        int cost1 = 0;
        if (DataManager.UnitDict.TryGetValue(unitId, out var towerData)) cost1 = towerData.stat.RequiredResources;
        if (DataManager.UnitDict.TryGetValue(unitId + 1, out var towerData2))
        {
            var cost2 = towerData2.stat.RequiredResources;
            cost = cost2 - cost1;
        }

        if (cost <= GameInfo.SheepResource)
        {
            GameInfo.SheepResource -= cost;
            return false;
        }
        
        return true;
    }

    private void SendWarningMessage(Player player, string msg)
    {
        S_SendWarningInGame warningPacket = new() { Warning = msg };
        player.Session.Send(warningPacket);
    }
}