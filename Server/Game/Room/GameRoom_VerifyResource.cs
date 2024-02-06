using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    private bool VerifyResourceForTower(Player player, int towerId)
    {
        if (!DataManager.TowerDict.TryGetValue(towerId, out var towerData)) return true;
        int resource = GameInfo.SheepResource;
        int cost = towerData.stat.RequiredResources;
        if (resource < cost)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        }
        
        GameInfo.SheepResource -= cost;
        return false;
    }
    
    private bool VerifyResourceForMonster(Player player, int monsterId)
    {
        int resource = GameInfo.WolfResource;
        if (!DataManager.MonsterDict.TryGetValue(monsterId, out var monsterData)) return true;
        int cost = monsterData.stat.RequiredResources;
        if (resource < cost)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        }
        
        GameInfo.WolfResource -= cost;
        return false;
    }

    private bool CheckUpgradeTowerPortrait(Player player, TowerId towerId)
    {
        int resource = GameInfo.SheepResource;
        int cost = VerifyUpgradeTowerPortrait(player, towerId);

        if (resource < cost)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        }

        GameInfo.SheepResource -= cost;
        return false;
    }

    private int VerifyUpgradeTowerPortrait(Player player, TowerId towerId)
    {
        GameData.OwnSkills.TryGetValue(towerId, out var skills);
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
    
    private bool CheckUpgradeMonsterPortrait(Player player, MonsterId monsterId)
    {
        return true;
    }
    
    private int VerifyUpgradeMonsterPortrait(Player player, MonsterId monsterId)
    {
        return 0;
    }
    
    private bool VerifyCapacityForTower(Player player, int towerId, SpawnWay way)
    {
        if (!DataManager.TowerDict.TryGetValue(towerId, out _)) return true;
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

        if (nowCapacity >= maxCapacity)
        {
            var warningMsg = "인구수를 초과했습니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        } 
        
        return false;
    }

    private bool VerifyCapacityForSheep(Player player)
    {
        if (GameInfo.SheepCount >= GameInfo.MaxSheep)
        {
            var warningMsg = "인구수를 초과했습니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        }

        return false;
    }
    
    private bool VerifyCapacityForMonster(Player player, int monsterId, SpawnWay way)
    {
        if (!DataManager.MonsterDict.TryGetValue(monsterId, out _)) return true;
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

        if (nowCapacity >= maxCapacity)
        {
            var warningMsg = "인구수를 초과했습니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        } 
        
        return false;
    }
    
    private bool VerifyResourceForSkill(Skill skill)
    {
        if (!DataManager.SkillDict.TryGetValue((int)skill, out var skillData)) return true;
        int cost = skillData.cost;
        if (GameInfo.SheepResource < cost) return true;
        GameInfo.SheepResource -= cost;
        return false;
    }

    private bool VerifySkillTree(Player player, Skill skill)
    {
        var skills = GameData.SkillTree[skill];
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
        int cost = VerifyFenceRepairCost();
        if (cost > GameInfo.SheepResource)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            Broadcast(warningPacket);
        }
        else
        {
            GameInfo.SheepResource -= cost;
            foreach (var fence in _fences)
            {
                fence.Value.Hp = fence.Value.MaxHp;
                Broadcast(new S_ChangeHp { ObjectId = fence.Key, Hp = fence.Value.Hp });
            }
        }
    }

    private bool VerifyUnitUpgrade(Player player, int towerId)
    {
        if (player.Portraits.Contains(towerId + 1)) return false;
        var warningMsg = "먼저 진화가 필요합니다.";
        S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
        player.Session.Send(warningPacket);
        return true;
    }

    private bool VerifyUnitUpgradeCost(Player player, int towerId)
    {
        int cost = 0;
        int cost1 = 0;
        int cost2 = 0;
        if (DataManager.TowerDict.TryGetValue(towerId, out var towerData)) cost1 = towerData.stat.RequiredResources;
        if (DataManager.TowerDict.TryGetValue(towerId + 1, out var towerData2))
        {
            cost2 = towerData2.stat.RequiredResources;
            cost = cost2 - cost1;
        }
        else
        {
            var warningMsg = "더 이상 진화할 수 없습니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return true;
        }

        if (cost <= GameInfo.SheepResource)
        {
            GameInfo.SheepResource -= cost;
            return false;
        }
        
        var warningMsg2 = "골드가 부족합니다.";
        S_SendWarningInGame warningPacket2 = new() { Warning = warningMsg2 };
        player.Session.Send(warningPacket2);
        return true;
    }
}