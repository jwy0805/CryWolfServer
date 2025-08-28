using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    public void HandleBaseSkillRun(Player? player, C_BaseSkillRun skillPacket)
    {
        if (player == null || _storage == null || _portal == null) return;
        var skill = skillPacket.Skill;
        int cost = CheckBaseSkillCost(skill);
        bool lackOfCost = GameInfo.SheepResource <= cost;
        if (lackOfCost)
        {
            SendWarningMessage(player, "warning_in_game_lack_of_gold");
            return;
        }
        
        switch (skill)
        {
            case Skill.RepairSheep:
                GameInfo.SheepResource -= cost;
                RepairFences(_fences.Values.ToList());
                break;
            
            case Skill.RepairWolf:
                GameInfo.WolfResource -= cost;
                RepairStatues(_statues.Values.ToList());
                break;
                
            case Skill.BaseUpgradeSheep:
                if (_storage.Level < 3)
                {
                    GameInfo.SheepResource -= cost;
                    _storage.LevelUp();
                }
                else
                {
                    SendWarningMessage(player, "warning_in_game_reached_max_level");
                }
                break;
            
            case Skill.BaseUpgradeWolf:
                if (_portal.Level < 3)
                {
                    GameInfo.WolfResource -= cost;
                    _portal.LevelUp();
                }
                else
                {
                    SendWarningMessage(player, "warning_in_game_reached_max_level");
                }
                break;
            
            case Skill.ResourceSheep:
                GameInfo.SheepResource -= cost;
                GameInfo.SheepYieldParam *= 1.3f;
                GameInfo.SheepYieldUpgradeCost = (int)(GameInfo.SheepYieldUpgradeCost * 1.5f);
                break;
            
            case Skill.ResourceWolf:
                GameInfo.WolfResource -= cost;
                GameInfo.WolfYieldParam *= 1.3f;
                GameInfo.WolfYieldUpgradeCost = (int)(GameInfo.WolfYieldUpgradeCost * 1.5f);
                break;
            
            case Skill.AssetSheep:
                GameInfo.SheepResource -= cost;
                SpawnSheep(player);
                break;
            
            case Skill.AssetWolf:
                if (Enchant is not { EnchantLevel: < 5 })
                {
                    SendWarningMessage(player, "warning_in_game_reached_max_level");
                    return;
                }
                GameInfo.WolfResource -= cost;
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
            SendWarningMessage(player, "warning_in_game_already_learn_skill");
            return;
        }
        
        if (lackOfSkill)
        {
            SendWarningMessage(player, "warning_in_game_missing_prerequisite_skill");
            return;
        }

        if (lackOfCost)
        {
            SendWarningMessage(player, "warning_in_game_lack_of_gold");
            return;
        }

        if (DataManager.SkillDict.TryGetValue((int)skill, out var skillData))
        {
            if (player.Faction == Faction.Sheep) GameInfo.SheepResource -= skillData.cost;
            else GameInfo.WolfResource -= skillData.cost;
        }
        
        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
        player.Session?.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandlePortraitUpgrade(Player? player, C_PortraitUpgrade upgradePacket)
    {
        if (player == null) return;

        var prevUnitId = upgradePacket.UnitId;
        var upgradeUnitId = (UnitId)((int)prevUnitId + 1);
        
        DataManager.UnitDict.TryGetValue((int)prevUnitId, out var unitData);
        if (unitData == null) return;
        if(Enum.TryParse(unitData.faction, out Faction faction) == false) return;
        if (player.AvailableUnits.Contains(upgradeUnitId) == false) return;
        
        var lackOfGold = faction == Faction.Sheep 
            ? VerifyUpgradeTowerPortrait(player, prevUnitId) 
            : VerifyUpgradeMonsterPortrait(player, prevUnitId);
        
        if (lackOfGold == false)
        {
            UpdateRemainSkills(player, prevUnitId);
            player.Portraits.Add((int)upgradeUnitId);
            player.Session?.Send(new S_PortraitUpgrade { UnitId = upgradeUnitId });
        }
        else
        {
            SendWarningMessage(player, "warning_in_game_lack_of_gold");
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
                bool evolutionEnded = !DataManager.UnitDict.TryGetValue((int)originalTower.UnitId + 1, out _);
                bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)originalTower.UnitId);
                bool lackOfCost = VerifyUnitUpgradeCost((int)originalTower.UnitId);
            
                if (evolutionEnded)
                {
                    SendWarningMessage(player, "warning_in_game_cannot_evolve_further");
                    return;
                }

                if (lackOfUpgrade)
                {
                    SendWarningMessage(player, "warning_in_game_needs_to_evolve");
                    return;
                }

                if (lackOfCost)
                {
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
            }

            if (go is MonsterStatue originalStatue)
            {
                bool evolutionEnded = !DataManager.UnitDict.TryGetValue((int)originalStatue.UnitId+ 1, out _);
                bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)originalStatue.UnitId);
                bool lackOfCost = VerityStatueUpgradeCost((int)originalStatue.UnitId);
            
                if (evolutionEnded)
                {
                    SendWarningMessage(player, "warning_in_game_cannot_evolve_further");
                    return;
                }

                if (lackOfUpgrade)
                {
                    SendWarningMessage(player, "warning_in_game_needs_to_evolve");
                    return;
                }

                if (lackOfCost)
                {
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
            }

            UpgradeUnit(go, player);
        }
    }

    public void HandleUnitRepair(Player? player, C_UnitRepair packet)
    {
        if (player == null) return;
        var unitIds = packet.RepairAll 
            ? _fences.Values.Select(fence => fence.Id).ToArray() 
            : packet.ObjectId.ToArray();
        
        foreach (var unitId in unitIds)
        {
            var go = FindGameObjectById(unitId);
            if (go == null) continue;
            if (go.ObjectType == GameObjectType.Fence)
            {
                var fence = go as Fence;
                var cost = CalcFenceRepairCost(new[] { unitId });
                if (GameInfo.SheepResource < cost)
                {
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
                GameInfo.SheepResource -= cost;
                if (fence != null) RepairFences(new List<Fence> { fence });
            }
            else
            {
                var statue = go as MonsterStatue;
                var cost = CalcStatueRepairCost(new[] { unitId });
                if (GameInfo.WolfResource < cost)
                {
                    SendWarningMessage(player, "warning_in_game_lack_of_gold");
                    return;
                }
                GameInfo.WolfResource -= cost;
                if (statue != null) RepairStatues(new List<MonsterStatue> { statue });
            }
        }
    }

    public void HandleSetUpgradePopup(Player? player, C_SetUpgradePopup packet)
    {
        DataManager.SkillDict.TryGetValue(packet.SkillId, out var skillData);
        if (skillData == null || player == null) return;
        var skillInfo = new SkillInfo { Id = skillData.id, Cost = skillData.cost };
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
        var cost = packet.Faction == Faction.Sheep ? CalcFenceRepairCost(ids) : CalcStatueRepairCost(ids);
        var costAll = packet.Faction == Faction.Sheep ? CalcFenceRepairCost(ids, true) : CalcStatueRepairCost(ids, true);
        var costPacket = new S_SetUnitRepairCost { Cost = cost, CostAll = costAll };
        player.Session?.Send(costPacket);
    }
    
    public void HandleSetBaseSkillCost(Player? player, C_SetBaseSkillCost packet)
    {
        if (player == null) return;
        
        var costArray = new int[4];
        if (packet.Faction == Faction.Sheep)
        {
            costArray[0] = CheckBaseSkillCost(Skill.BaseUpgradeSheep);
            costArray[1] = CheckBaseSkillCost(Skill.RepairSheep);
            costArray[2] = CheckBaseSkillCost(Skill.ResourceSheep);
            costArray[3] = CheckBaseSkillCost(Skill.AssetSheep);
        }
        else
        {
            costArray[0] = CheckBaseSkillCost(Skill.BaseUpgradeWolf);
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
        
        var objectIds = deletePacket.ObjectIds.ToArray();
        foreach (var objectId in objectIds)
        {
            var gameObject = FindGameObjectById(objectId);
            if (gameObject == null) return;

            switch (gameObject.ObjectType)
            {
                case GameObjectType.Tower:
                    if (gameObject is not Tower tower) return;
                    GameInfo.SheepResource += CalcUnitDeleteCost(new[] { objectId });
                    GameInfo.NorthTower--;
                    break;
                case GameObjectType.MonsterStatue:
                    if (gameObject is not MonsterStatue statue) return;
                    GameInfo.WolfResource += CalcUnitDeleteCost(new[] { objectId });
                    GameInfo.NorthMonster--;
                    break;
            }
            
            LeaveGame(objectId);
            Broadcast(new S_Despawn { ObjectIds = { objectId } });    
        }
    }
}