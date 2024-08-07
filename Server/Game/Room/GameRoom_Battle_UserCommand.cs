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
        var go = FindGameObjectById(upgradePacket.ObjectId);
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
            if (go is not Tower t) return;
            PositionInfo newTowerPos = new()
            {
                PosX = t.PosInfo.PosX, PosY = t.PosInfo.PosY, PosZ = t.PosInfo.PosZ, State = State.Idle
            };
            LeaveGame(id);
            Broadcast(new S_Despawn { ObjectIds = { id } });
            var towerId = t.UnitId + 1;
            var tower = SpawnTower(towerId, newTowerPos, player);
            Push(UpgradeTower, t, tower);
            player.Session?.Send(new S_UpgradeSlot
            {
                OldObjectId = id, NewObjectId = tower.Id, UnitId = (int)towerId
            });
        }
        else if (go.ObjectType == GameObjectType.Monster)
        {
            if (go is not Monster m) return;
            int statueId = m.StatueId;
            var statue = FindGameObjectById(statueId);
            if (statue == null) return;
            PositionInfo newStatuePos = new()
            {
                PosX = statue.PosInfo.PosX, PosY = statue.PosInfo.PosY, PosZ = statue.PosInfo.PosZ
            };
            LeaveGame(statueId);
            Broadcast(new S_Despawn { ObjectIds = { statueId } });
            var monsterId = m.UnitId + 1;
            var monsterStatue = SpawnMonsterStatue(monsterId, newStatuePos, player);
            Push(UpgradeMonsterStatue, (MonsterStatue)statue, monsterStatue);
            player.Session?.Send(new S_UpgradeSlot
            {
                OldObjectId = statueId, NewObjectId = monsterStatue.Id, UnitId = (int)monsterId
            });
        }
        else if (go.ObjectType == GameObjectType.MonsterStatue)
        {
            if (go is not MonsterStatue ms) return;
            PositionInfo newStatuePos = new()
            {
                PosX = ms.PosInfo.PosX, PosY = ms.PosInfo.PosY, PosZ = ms.PosInfo.PosZ
            };
            LeaveGame(id);
            Broadcast(new S_Despawn { ObjectIds = { id } });
            var monsterId = ms.UnitId + 1;
            var monsterStatue = SpawnMonsterStatue(monsterId, newStatuePos, player);
            Push(UpgradeMonsterStatue, ms, monsterStatue);
            player.Session?.Send(new S_UpgradeSlot
            {
                OldObjectId = id, NewObjectId = monsterStatue.Id, UnitId = (int)monsterId
            });
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

    public void HandleSetUpgradeButton(Player? player, C_SetUpgradeButton packet)
    {
        if (player == null) return;
        var cost = VerifyUpgradePortrait(player, (UnitId)packet.UnitId);
        S_SetUpgradeButton buttonPacket = new() { UnitId = packet.UnitId, Cost = cost };
        player.Session?.Send(buttonPacket);
    }
    
    public void HandleDelete(Player? player, C_DeleteUnit deletePacket)
    {
        if (player == null) return;
        
        int objectId = deletePacket.ObjectId;
        var gameObject = FindGameObjectById(objectId);
        if (gameObject == null) return;
        var type = gameObject.ObjectType;
        
        switch (type)
        {
            case GameObjectType.Tower:
                if (gameObject.Way == SpawnWay.North)
                {
                    TowerSlot? slotToBeDeleted = _northTowers.FirstOrDefault(slot => slot.ObjectId == objectId);
                    if (slotToBeDeleted is not null) _northTowers.Remove((TowerSlot)slotToBeDeleted);
                }
                else if (gameObject.Way == SpawnWay.South)
                {
                    TowerSlot? slotToBeDeleted = _southTowers.FirstOrDefault(slot => slot.ObjectId == objectId);
                    if (slotToBeDeleted is not null) _southTowers.Remove((TowerSlot)slotToBeDeleted);
                }
                break;
            
            case GameObjectType.Monster:
                if (gameObject is not Monster monster) return;
                int statueId = monster.StatueId;
                if (monster.Way == SpawnWay.North)
                {
                    MonsterSlot? slotToBeDeleted = _northMonsters.FirstOrDefault(slot => slot.Statue.Id == statueId);
                    if (slotToBeDeleted is not null) _northMonsters.Remove((MonsterSlot)slotToBeDeleted);
                }
                else if (gameObject.Way == SpawnWay.South)
                {
                    MonsterSlot? slotToBeDeleted = _southMonsters.FirstOrDefault(slot => slot.Statue.Id == statueId);
                    if (slotToBeDeleted is not null) _southMonsters.Remove((MonsterSlot)slotToBeDeleted);
                }
                break;
            
            case GameObjectType.MonsterStatue:
                if (gameObject.Way == SpawnWay.North)
                {
                    MonsterSlot? slotToBeDeleted = _northMonsters.FirstOrDefault(slot => slot.Statue.Id == objectId);
                    if (slotToBeDeleted is not null) _northMonsters.Remove((MonsterSlot)slotToBeDeleted);
                }
                else if (gameObject.Way == SpawnWay.South)
                {
                    MonsterSlot? slotToBeDeleted = _southMonsters.FirstOrDefault(slot => slot.Statue.Id == objectId);
                    if (slotToBeDeleted is not null) _southMonsters.Remove((MonsterSlot)slotToBeDeleted);
                }
                break;
            
            default:
                return;
        }
        
        LeaveGame(objectId);
        Broadcast(new S_Despawn { ObjectIds = { objectId } });

        if (deletePacket.Inactive == false) return;
    }
}