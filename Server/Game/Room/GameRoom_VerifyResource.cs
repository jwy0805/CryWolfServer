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
    
    private bool VerifyResourceForSkill(Player player, Skill skill)
    {
        if (!DataManager.SkillDict.TryGetValue((int)skill, out var skillData)) return true;
        int resource = GameInfo.SheepResource;
        int cost = skillData.cost;
        if (resource <= cost) return true;
        GameInfo.SheepResource =- cost;
        return false;
    }

    private bool VerifySkillTree(Player player, Skill skill)
    {
        var skills = GameData.SkillTree[skill];
        return skills.All(item => player.SkillUpgradedList.Contains(item));
    }
}