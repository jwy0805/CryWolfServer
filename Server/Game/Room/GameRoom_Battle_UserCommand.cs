using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom
{
    public void HandleBaseSkillRun(Player? player, C_BaseSkillRun skillPacket)
    {
        if (player == null || _storage == null || _portal == null) return;
        var skill = skillPacket.Skill;
        int cost = CheckBaseSkillCost(player, skill);
        bool lackOfCost = player.Faction == Faction.Sheep ? GameInfo.SheepResource < cost : GameInfo.WolfResource < cost;
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
            if (player.Faction == Faction.Sheep) GameInfo.SheepResource -= skillData.Cost;
            else GameInfo.WolfResource -= skillData.Cost;
        }
        
        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
        player.Session?.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
    {
        if (player == null) return;

        var prevUnitId = upgradePacket.UnitId;
        var upgradeUnitId = (UnitId)((int)prevUnitId + 1);
        
        DataManager.UnitDict.TryGetValue((int)prevUnitId, out var unitData);
        if (unitData == null) return;
        if(Enum.TryParse(unitData.Faction, out Faction faction) == false) return;
        if (player.AvailableUnits.Contains(upgradeUnitId) == false) return;
        if ((int)upgradeUnitId % 100 % 3 == 0 && GetBaseLevel(faction) < 2)
        {
            SendWarningMessage(player, "warning_in_game_lack_of_base_level");
            
            // Tutorial
            _tutorialTrigger.TryTrigger(player, player.Faction,
                $"Battle{player.Faction}.InfoUnitUpgradeCondition", 
                true, 
                () => true);

            return;
        }
        
        var lackOfGold = faction == Faction.Sheep 
            ? VerifyUpgradeTowerPortrait(player, prevUnitId) 
            : VerifyUpgradeMonsterPortrait(player, prevUnitId);
        
        if (lackOfGold == false)
        {
            UpdateRemainSkills(player, prevUnitId);
            if (player.Faction == Faction.Sheep)
            {
                UpgradeTowers(prevUnitId, player);
            }
            else
            {
                UpgradeStatues(prevUnitId, player);
            }
            player.UpdateCurrentUnits(prevUnitId, upgradeUnitId);
            player.Session?.Send(new S_UnitUpgrade { UnitId = upgradeUnitId });
        }
        else
        {
            SendWarningMessage(player, "warning_in_game_lack_of_gold");
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
        var skillInfo = new SkillInfo { Id = skillData.Id, Cost = skillData.Cost };
        S_SetUpgradePopup popupPacket = new() { SkillInfo = skillInfo };
        player.Session?.Send(popupPacket);
    }

    public void HandleSetCostInUpgradeButton(Player? player, C_SetUpgradeCost packet)
    {
        if (player == null) return;
        var cost = CalcUpgradePortrait(player, (UnitId)packet.UnitId);
        S_SetUpgradeCost buttonPacket = new() { Cost = cost };
        player.Session?.Send(buttonPacket);
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
            costArray[0] = CheckBaseSkillCost(player, Skill.BaseUpgradeSheep);
            costArray[1] = CheckBaseSkillCost(player, Skill.RepairSheep);
            costArray[2] = CheckBaseSkillCost(player, Skill.ResourceSheep);
            costArray[3] = CheckBaseSkillCost(player, Skill.AssetSheep);
        }
        else
        {
            costArray[0] = CheckBaseSkillCost(player, Skill.BaseUpgradeWolf);
            costArray[1] = CheckBaseSkillCost(player, Skill.RepairWolf);
            costArray[2] = CheckBaseSkillCost(player, Skill.ResourceWolf);
            costArray[3] = CheckBaseSkillCost(player, Skill.AssetWolf);
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
