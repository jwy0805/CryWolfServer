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
            case Skill.FenceRepair:
                GameInfo.SheepResource -= cost;
                FenceRepair();
                break;
            
            case Skill.StorageLvUp:
                GameInfo.SheepResource -= cost;
                StorageLevel = 2;
                break;
            
            case Skill.GoldIncrease:
                GameInfo.SheepResource -= cost;
                GameInfo.SheepYield *= 2;
                break;
            
            case Skill.SheepHealth:
                GameInfo.SheepResource -= cost;
                foreach (var sheep in _sheeps.Values)
                {
                    sheep.MaxHp *= 2;
                    sheep.Hp += sheep.MaxHp / 2;
                }
                break;
            
            case Skill.SheepIncrease:
                bool lackOfSheepCapacity = GameInfo.SheepCount >= GameInfo.MaxSheep;
                if (lackOfSheepCapacity == false)
                {
                    GameInfo.SheepResource -= cost;
                    SpawnSheep(player);
                }
                else
                {
                    SendWarningMessage(player, "인구수를 초과했습니다.");
                }
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
        lackOfCost = VerifyResourceForSkill(player, skill);

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
        if(Enum.TryParse(unitData.camp, out Camp camp) == false) return;
        
        var lackOfGold = camp == Camp.Sheep 
            ? CalcUpgradeTowerPortrait(player, unitId) 
            : CalcUpgradeMonsterPortrait(player, unitId);
        
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
            if (go is not Tower originTower) return;
            
            // 실제 환경
            bool evolutionEnded = !DataManager.UnitDict.TryGetValue((int)originTower.UnitId+ 1, out _);
            bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)originTower.UnitId);
            bool lackOfCost = VerifyUnitUpgradeCost((int)originTower.UnitId);
            
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

    public void HandleUnitRepair(Player? player, C_UnitRepair packet)
    {
        
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
        var cost = VerifyUpgradePortrait(player, (UnitId)packet.UnitId);
        S_SetUpgradeButtonCost buttonPacket = new() { Cost = cost };
        player.Session?.Send(buttonPacket);
    }
    
    public void HandleUnitUpgradeCost(Player? player, C_SetUnitUpgradeCost packet)
    {
        if (player == null) return;
    }
    
    public void HandleUnitDeleteCost(Player? player, C_SetUnitDeleteCost packet)
    {
        if (player == null) return;
    }
    
    public void HandleUnitRepairCost(Player? player, C_SetUnitRepairCost packet)
    {
        if (player == null) return;
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