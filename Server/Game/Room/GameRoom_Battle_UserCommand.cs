using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    public void HandleBaseSkillRun(Player? player, C_BaseSkillRun skillPacket)
    {
        if (player == null) return;
        var skill = skillPacket.Skill;
        int cost = CheckBaseSkillCost(skill);
        bool lackOfCost = GameInfo.SheepResource <= cost;
        if (lackOfCost)
        {
            SendWarningMessage(player, "골드가 부족합니다.");
            return;
        }
        
        switch (skill)
        {
            case Skill.RepairSheep:
                GameInfo.SheepResource -= cost;
                RepairAllFences();
                break;
            
            case Skill.RepairWolf:
                GameInfo.WolfResource -= cost;
                RepairAllStatues();
                break;
                
            case Skill.UpgradeSheep:
                if (StorageLevel <= 3)
                {
                    GameInfo.SheepResource -= cost;
                    StorageLevel++;
                }
                break;
            
            case Skill.UpgradeWolf:
                if (StorageLevel <= 3)
                {
                    GameInfo.WolfResource -= cost;
                    StorageLevel++;
                }
                break;
            
            case Skill.ResourceSheep:
                GameInfo.SheepResource -= cost;
                GameInfo.SheepYieldParam *= 1.3f;
                GameInfo.SheepYieldUpgradeCost = (int)(GameInfo.SheepYieldUpgradeCost * 1.5f);
                break;
            
            case Skill.ResourceWolf:
                GameInfo.WolfResource -= cost;
                GameInfo.WolfYield *= 2;
                break;
            
            case Skill.AssetSheep:
                GameInfo.SheepResource -= cost;
                SpawnSheep(player);
                break;
            
            case Skill.AssetWolf:
                if (Enchant is not { EnchantLevel: < 5 }) return;
                Enchant.EnchantLevel++;
                break;
        }
    }

    public void HandleSkillUpgrade(Player? player, C_SkillUpgrade upgradePacket)
    {
        if (player == null) return;
        
        var skill = upgradePacket.Skill;
        bool lackOfSkill = false;
        bool lackOfCost = false;
        
        // 실제 환경
        lackOfSkill = VerifySkillTree(player, skill);
        lackOfCost = VerifyResourceForSkillUpgrade(player, skill);

        if (player.SkillUpgradedList.Contains(skill))
        {
            SendWarningMessage(player, "이미 스킬을 배웠습니다.");
            return;
        }
        
        if (lackOfSkill)
        {
            SendWarningMessage(player, "선행 스킬이 부족합니다.");
            return;
        }

        if (lackOfCost)
        {
            SendWarningMessage(player, "골드가 부족합니다.");
            return;
        }

        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
        player.Session?.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandlePortraitUpgrade(Player? player, C_PortraitUpgrade upgradePacket)
    {
        if (player == null) return;

        var unitId = upgradePacket.UnitId;
        DataManager.UnitDict.TryGetValue((int)unitId, out var unitData);
        if (unitData == null) return;
        if(Enum.TryParse(unitData.faction, out Faction faction) == false) return;
        
        var lackOfGold = faction == Faction.Sheep 
            ? VerifyUpgradeTowerPortrait(player, unitId) 
            : VerifyUpgradeMonsterPortrait(player, unitId);
        
        if (lackOfGold == false)
        {
            var newUnitId = (UnitId)((int)unitId + 1);
            player.Portraits.Add((int)newUnitId);
            player.Session?.Send(new S_PortraitUpgrade { UnitId = newUnitId });
        }
        else
        {
            SendWarningMessage(player, "골드가 부족합니다.");
        }
    }

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
    {
        if (player == null) return;
        var objectIds = upgradePacket.ObjectId.ToArray();
        foreach (var objectId in objectIds)
        {
            var go = FindGameObjectById(objectId);
            if (go == null) return;

            if (go is Tower originalTower)
            {
                bool evolutionEnded = !DataManager.UnitDict.TryGetValue((int)originalTower.UnitId+ 1, out _);
                bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)originalTower.UnitId);
                bool lackOfCost = VerifyUnitUpgradeCost((int)originalTower.UnitId);
            
                if (evolutionEnded)
                {
                    SendWarningMessage(player, "더 이상 진화할 수 없습니다.");
                    return;
                }

                if (lackOfUpgrade)
                {
                    SendWarningMessage(player, "먼저 진화가 필요합니다.");
                    return;
                }

                if (lackOfCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
            }

            if (go is MonsterStatue originalStatue)
            {
                bool evolutionEnded = !DataManager.UnitDict.TryGetValue((int)originalStatue.UnitId+ 1, out _);
                bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)originalStatue.UnitId);
                bool lackOfCost = VerifyUnitUpgradeCost((int)originalStatue.UnitId);
            
                if (evolutionEnded)
                {
                    SendWarningMessage(player, "더 이상 진화할 수 없습니다.");
                    return;
                }

                if (lackOfUpgrade)
                {
                    SendWarningMessage(player, "먼저 진화가 필요합니다.");
                    return;
                }

                if (lackOfCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
            }

            int id = go.Id;
            if (go.ObjectType == GameObjectType.Tower)
            {
                if (go is not Tower tower) return;
                PositionInfo newTowerPos = new()
                {
                    PosX = tower.PosInfo.PosX, PosY = tower.PosInfo.PosY, PosZ = tower.PosInfo.PosZ, State = State.Idle
                };
                LeaveGame(id);
                Broadcast(new S_Despawn { ObjectIds = { id } });
                var towerId = tower.UnitId + 1;
                SpawnTower(towerId, newTowerPos, player);
            }
            else if (go.ObjectType == GameObjectType.Monster)
            {
                if (go is not Monster monster) return;
                var statueId = monster.StatueId;
                var statue = FindGameObjectById(statueId);
                if (statue == null) return;
                PositionInfo newStatuePos = new()
                {
                    PosX = statue.PosInfo.PosX, PosY = statue.PosInfo.PosY, PosZ = statue.PosInfo.PosZ
                };
                LeaveGame(statueId);
                Broadcast(new S_Despawn { ObjectIds = { statueId } });
                var monsterId = monster.UnitId + 1;
                SpawnMonsterStatue(monsterId, newStatuePos, player);
            }
            else if (go.ObjectType == GameObjectType.MonsterStatue)
            {
                if (go is not MonsterStatue statue) return;
                PositionInfo newStatuePos = new()
                {
                    PosX = statue.PosInfo.PosX, PosY = statue.PosInfo.PosY, PosZ = statue.PosInfo.PosZ
                };
                LeaveGame(id);
                Broadcast(new S_Despawn { ObjectIds = { id } });
                var monsterId = statue.UnitId + 1;
                SpawnMonsterStatue(monsterId, newStatuePos, player);
            }
        }
    }

    public void HandleSetUpgradePopup(Player? player, C_SetUpgradePopup packet)
    {
        DataManager.SkillDict.TryGetValue(packet.SkillId, out var skillData);
        if (skillData == null || player == null) return;

        var skillInfo = new SkillInfo { Explanation = skillData.explanation, Cost = skillData.cost };
        S_SetUpgradePopup popupPacket = new() { SkillInfo = skillInfo };
        player.Session?.Send(popupPacket);
    }

    public void HandleSetCostInUpgradeButton(Player? player, C_SetUpgradeButtonCost packet)
    {
        if (player == null) return;
        var cost = CalcUpgradePortrait(player, (UnitId)packet.UnitId);
        S_SetUpgradeButtonCost buttonPacket = new() { Cost = cost };
        player.Session?.Send(buttonPacket);
    }
    
    public void HandleSetUpgradeCostText(Player? player, C_SetUnitUpgradeCost packet)
    {
        if (player == null) return;
        var ids = packet.ObjectIds.ToArray();
        var cost = CalcUnitUpgradeCost(ids);
        var costPacket = new S_SetUnitUpgradeCost { Cost = cost };
        player.Session?.Send(costPacket);
    }
    
    public void HandleSetDeleteCostText(Player? player, C_SetUnitDeleteCost packet)
    {
        if (player == null) return;
        var ids = packet.ObjectIds.ToArray();
        var cost = CalcUnitDeleteCost(ids);
        var costPacket = new S_SetUnitDeleteCost { Cost = cost };
        player.Session?.Send(costPacket);
    }
    
    public void HandleSetRepairCostText(Player? player, C_SetUnitRepairCost packet)
    {
        if (player == null) return;
        var ids = packet.ObjectIds.ToArray();
        var cost = CalcFenceRepairCost(ids);
        if (cost == 0) return;
        var costPacket = new S_SetUnitRepairCost { Cost = cost };
        player.Session?.Send(costPacket);
    }
    
    public void HandleSetBaseSkillCost(Player? player, C_SetBaseSkillCost packet)
    {
        if (player == null) return;
        
        var costArray = new int[4];
        if (packet.Faction == Faction.Sheep)
        {
            costArray[0] = CheckBaseSkillCost(Skill.UpgradeSheep);
            costArray[1] = CheckBaseSkillCost(Skill.RepairSheep);
            costArray[2] = CheckBaseSkillCost(Skill.ResourceSheep);
            costArray[3] = CheckBaseSkillCost(Skill.AssetSheep);
        }
        else
        {
            costArray[0] = CheckBaseSkillCost(Skill.UpgradeWolf);
            costArray[1] = CheckBaseSkillCost(Skill.RepairWolf);
            costArray[2] = CheckBaseSkillCost(Skill.ResourceWolf);
            costArray[3] = CheckBaseSkillCost(Skill.AssetWolf);
        }

        var costPacket = new S_SetBaseSkillCost
        {
            UpgradeCost = costArray[0],
            RepairCost = costArray[1],
            ResourceCost = costArray[2],
            AssetCost = costArray[3]
        };
        
        player.Session?.Send(costPacket);
    }
    
    public void HandleDelete(Player? player, C_UnitDelete deletePacket)
    {
        if (player == null) return;
        
        var objectIds = deletePacket.ObjectId.ToArray();
        foreach (var objectId in objectIds)
        {
            var gameObject = FindGameObjectById(objectId);
            if (gameObject == null) return;
        
            LeaveGame(objectId);
            Broadcast(new S_Despawn { ObjectIds = { objectId } });    
        }
    }
}