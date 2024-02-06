using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Util;

namespace Server.Game;

public partial class GameRoom
{
    public readonly Stopwatch Stopwatch = new();
    
    public int StorageLevel
    {
        get => _storageLevel;
        set
        {
            _storageLevel = value;
            if (_storageLevel > GameInfo.MaxStorageLevel)
            {
                _storageLevel = GameInfo.MaxStorageLevel;
                return;
            }
            GameInfo.StorageLevel = _storageLevel;
            
            // 인구수 증가
            if (_storageLevel == 1)
            {
                GameInfo.MaxSheep = 5;
                GameInfo.NorthMaxTower = 6;
                GameInfo.SouthMaxTower = 6;
            }
            else if (_storageLevel == 2)
            {
                GameInfo.MaxSheep = 8;
                GameInfo.NorthMaxTower = 9;
                GameInfo.SouthMaxTower = 9;
                GameInfo.SheepYield += 20;
            }
            
            // 울타리 생성
            if (_storageLevel != 1 && _fences.Count > 0)
            {
                // 기존 울타리 삭제
                List<int> deleteFences = _fences.Keys.ToList();
                foreach (var fenceId in deleteFences)
                {
                    LeaveGame(fenceId);
                    Broadcast(new S_Despawn { ObjectIds = { fenceId } });
                }
                _fences.Clear();
            }
            
            SpawnFence(_storageLevel);
        }
    }
    
    private void GameInit()
    {
        Stopwatch.Start();
        _timeSendTime = Stopwatch.ElapsedMilliseconds;
        BaseInit();
        BuffManager.Instance.Room = this;
        BuffManager.Instance.Update();
    }
    
    public void HandlePlayerMove(Player? player, C_PlayerMove pMovePacket)
    {
        if (player == null) return;
        
        S_PlayerMove playerMovePacket = new S_PlayerMove
        {
            State = pMovePacket.State,
            ObjectId = player.Id,
            DestPos = pMovePacket.DestPos
        }; 
        
        Broadcast(playerMovePacket);
    }
    
    public void HandleMove(Player? player, C_Move movePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(movePacket.ObjectId);
        if (go == null) return;
        
        Vector3 v = new Vector3(movePacket.PosX, movePacket.PosY, movePacket.PosZ);
        Vector3 cellPos = Util.Util.NearestCell(v);
        if (go.ObjectType == GameObjectType.Player) go.CellPos = cellPos;
        else go.ApplyMap(cellPos);
    }
    
    public void HandleSetDest(Player? player, C_SetDest destPacket)
    {
        if (player == null) return;
        GameObjectType type = ObjectManager.GetObjectTypeById(destPacket.ObjectId);
        if (type == GameObjectType.Projectile)
        {
            Projectile? p = FindGameObjectById(destPacket.ObjectId) as Projectile;
            p?.BroadcastDest();
        }
        else
        {
            GameObject? go = FindGameObjectById(destPacket.ObjectId);
            go?.BroadcastDest();
        }
    }
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket) // 클라이언트의 요청으로 Spawn되는 경우
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;
        
        switch (type)
        {
            case GameObjectType.Tower:
                if (!Enum.IsDefined(typeof(TowerId), spawnPacket.Num)) return;
                bool lackOfTowerCost = VerifyResourceForTower(player, spawnPacket.Num);
                bool lackOfTowerCapacity = VerifyCapacityForTower(player, spawnPacket.Num, spawnPacket.Way);
                if (lackOfTowerCost || lackOfTowerCapacity) return;
                var tower = EnterTower(spawnPacket.Num, spawnPacket.PosInfo, player);
                if (spawnPacket.Register) RegisterTower(tower);
                Push(EnterGame, tower);
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                Monster monster = EnterMonster(spawnPacket.Num, spawnPacket.PosInfo, player);
                Push(EnterGame, monster);
                break;
            
            case GameObjectType.MonsterStatue:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                bool lackOfMonsterCost = VerifyResourceForMonster(player, spawnPacket.Num);
                bool lackOfMonsterCapacity = VerifyCapacityForMonster(player, spawnPacket.Num, spawnPacket.Way);
                if (lackOfMonsterCost || lackOfMonsterCapacity) return;
                MonsterStatue monsterStatue = EnterMonsterStatue(spawnPacket.Num, spawnPacket.PosInfo, player);
                RegisterMonsterStatue(monsterStatue);
                Push(EnterGame, monsterStatue);
                break;
            
            case GameObjectType.Sheep:
                var sheep = EnterSheep(player);
                Push(EnterGame, sheep);
                GameInfo.SheepCount++;
                break;
            
            case GameObjectType.Effect:
                EffectId effectType = (EffectId)spawnPacket.Num;
                Effect effect = ObjectManager.Instance.CreateEffect(effectType);
                effect.PosInfo = spawnPacket.PosInfo;
                effect.Info.PosInfo = effect.PosInfo;
                effect.Init();
                Push(EnterGame, effect);
                break;
        }
    }

    public void HandleState(Player? player, C_State statePacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(statePacket.ObjectId);
        if (go == null) return;
        go.State = statePacket.State;
    }
    
    public void HandleAttack(Player? player, C_Attack attackPacket)
    {
        if (player == null) return;
        int attackerId = attackPacket.ObjectId;
        GameObject? parent = FindGameObjectById(attackerId);
        GameObject? target = parent?.Target;
        
        if (parent == null) return;
        GameObjectType type = parent.ObjectType;

        if (attackPacket.AttackMethod == AttackMethod.EffectAttack)
        {
            Effect effect = ObjectManager.Instance.CreateEffect(attackPacket.Effect);
            effect.Room = this;
            effect.Parent = parent;
            effect.Info.Name = attackPacket.Effect.ToString();
            effect.EffectId = attackPacket.Effect;
            effect.PosInfo = effect.SetEffectPos(parent);
            effect.Info.PosInfo = effect.PosInfo;
            effect.Init();
            Push(EnterGameParent, effect, effect.Parent);
        }

        if (target == null || target.Targetable == false)
        {
            if (type is not (GameObjectType.Tower or GameObjectType.Monster)) return;
            Creature cAttacker = (Creature)parent;
            cAttacker.SetNextState();
            return;
        }
        
        switch (attackPacket.AttackMethod)
        {
            case AttackMethod.NoAttack:
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)parent;
                    cAttacker.SetNormalAttackEffect(target);
                }
                break;
            case AttackMethod.NormalAttack:
                int damage = parent.TotalAttack;
                target.OnDamaged(parent, damage);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)parent;
                    cAttacker.SetNextState();
                    cAttacker.Mp += cAttacker.Stat.MpRecovery;
                    cAttacker.SetNormalAttackEffect(target);
                }
                else if (type is GameObjectType.Projectile)
                {
                    parent.Parent!.Mp += parent.Parent.Stat.MpRecovery;
                    Projectile? pAttacker = FindGameObjectById(attackerId) as Projectile;
                    pAttacker?.SetProjectileEffect(target);
                }
                break;
            
            case AttackMethod.ProjectileAttack:
                if (!Enum.IsDefined(typeof(ProjectileId), attackPacket.Projectile)) return;
                Projectile projectile = ObjectManager.Instance.CreateProjectile(attackPacket.Projectile);
                projectile.Room = this;
                projectile.PosInfo = parent.PosInfo;
                // projectile.PosInfo.PosY = attacker.PosInfo.PosY + attacker.Stat.SizeY;
                projectile.Info.PosInfo = projectile.PosInfo;
                projectile.Info.Name = attackPacket.Projectile.ToString();
                projectile.ProjectileId = attackPacket.Projectile;
                projectile.Target = target;
                projectile.Parent = parent;
                projectile.TotalAttack = parent.TotalAttack;
                projectile.Init();
                Push(EnterGame, projectile);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)parent;
                    cAttacker.SetNextState();
                }
                break;
        }
    }
    
    public void HandleEffectAttack(Player? player, C_EffectAttack dirPacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(dirPacket.ObjectId);
        if (go == null) return;
        var effect = (Effect)go;
        effect.PacketReceived = true;
    }
    
    public void HandleStatInit(Player? player, C_StatInit initPacket)
    {
        if (player == null) return;

        Creature? creature = FindGameObjectById(initPacket.ObjectId) as Creature;
        creature?.StatInit();
    }

    public void HandleSkillInit(Player? player, C_SkillInit initPacket)
    {
        if (player == null) return;
        
        Creature? creature = FindGameObjectById(initPacket.ObjectId) as Creature;
        creature?.SkillInit();
    }
    
    public void HandleSkill(Player? player, C_Skill skillPacket)
    {
        if (player == null) return;
        
        Creature? creature = FindGameObjectById(skillPacket.ObjectId) as Creature;
        creature?.RunSkill();
        creature?.SetNextState();
    }
    
    public void HandleBaseSkillRun(Player? player, C_BaseSkillRun skillPacket)
    {
        if (player == null) return;
        var skill = skillPacket.Skill;
        int cost = CheckBaseSkillCost(skill);
        bool lackOfCost = GameInfo.SheepResource <= cost;
        if (lackOfCost)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
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
                bool lackOfSheepCapacity = VerifyCapacityForSheep(player);
                if (lackOfSheepCapacity == false)
                {
                    GameInfo.SheepResource -= cost;
                    EnterSheepByServer(player);
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
        lackOfCost = VerifyResourceForSkill(skill);

        if (player.SkillUpgradedList.Contains(skill))
        {
            var warningMsg = "이미 스킬을 배웠습니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return;
        }
        
        if (lackOfSkill)
        {
            var warningMsg = "선행스킬이 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return;
        }

        if (lackOfCost)
        {
            var warningMsg = "골드가 부족합니다.";
            S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
            player.Session.Send(warningPacket);
            return;
        }



        player.SkillSubject.SkillUpgraded(skill);
        player.SkillUpgradedList.Add(skill);
        player.Session.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandlePortraitUpgrade(Player? player, C_PortraitUpgrade upgradePacket)
    {
        if (player == null) return;

        bool lackOfGold = false;
        TowerId towerId = TowerId.UnknownTower;
        MonsterId monsterId = MonsterId.UnknownMonster;
        if (upgradePacket.MonsterId == MonsterId.UnknownMonster)
        {
            towerId = upgradePacket.TowerId;
            lackOfGold = CheckUpgradeTowerPortrait(player, towerId);
        }
        else
        {
            monsterId = upgradePacket.MonsterId;
            lackOfGold = CheckUpgradeMonsterPortrait(player, monsterId);
        }
        
        if (lackOfGold == false)
        {
            if (monsterId == MonsterId.UnknownMonster)
            {
                towerId = (TowerId)((int)towerId + 1);
                player.Portraits.Add((int)towerId);
            }
            else
            {
                monsterId = (MonsterId)((int)monsterId + 1);
                player.Portraits.Add((int)monsterId);
            }
            player.Session.Send(new S_PortraitUpgrade { TowerId = towerId, MonsterId = monsterId });
        }
    }

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
    {
        if (player == null) return;
        var go = FindGameObjectById(upgradePacket.ObjectId);
        if (go is not Tower towerr) return;
        
        // 실제 환경
        bool lackOfUpgrade = VerifyUnitUpgrade(player, (int)towerr.TowerId);
        bool lackOfCost = VerifyUnitUpgradeCost(player, (int)towerr.TowerId);
        
        if (lackOfUpgrade)
        {
            return;
        }
        else if (lackOfCost)
        {
            return;
        }
        else
        {
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
                int towerId = (int)t.TowerId + 1;
                Tower tower = EnterTower(towerId, newTowerPos, player);
                
                Push(EnterGame, tower);
                UpgradeTower(t, tower);
                player.Session.Send(new S_UpgradeSlot { OldObjectId = id, NewObjectId = tower.Id, UnitId = towerId });
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
                int monsterId = (int)m.MonsterId + 1;
                MonsterStatue monsterStatue = EnterMonsterStatue(monsterId, newStatuePos, player);
                
                Push(EnterGame, monsterStatue);
                UpgradeMonsterStatue((MonsterStatue)statue, monsterStatue);
                player.Session.Send(new S_UpgradeSlot { OldObjectId = statueId, NewObjectId = monsterStatue.Id, UnitId = monsterId });
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
                int monsterId = (int)ms.MonsterId + 1;
                MonsterStatue monsterStatue = EnterMonsterStatue(monsterId, newStatuePos, player);
                
                Push(EnterGame, monsterStatue);
                UpgradeMonsterStatue(ms, monsterStatue);
                player.Session.Send(new S_UpgradeSlot { OldObjectId = id, NewObjectId = monsterStatue.Id, UnitId = monsterId });
            }
        }
    }

    public void HandleSetUpgradePopup(Player? player, C_SetUpgradePopup packet)
    {
        int skillId = packet.SkillId;
        DataManager.SkillDict.TryGetValue(packet.SkillId, out var skillData);
        if (skillData == null) return;

        var skillInfo = skillId is >= 700 and < 900 
            ? new SkillInfo {Explanation = skillData.explanation, Cost = CheckBaseSkillCost((Skill)skillId)} 
            : new SkillInfo { Explanation = skillData.explanation, Cost = skillData.cost };
        S_SetUpgradePopup popupPacket = new() { SkillInfo = skillInfo };
        player?.Session.Send(popupPacket);
    }

    public void HandleSetUpgradeButton(Player? player, C_SetUpgradeButton packet)
    {
        if (player == null) return;
        var cost = player.Camp == Camp.Sheep 
            ? VerifyUpgradeTowerPortrait(player, (TowerId)packet.UnitId) 
            : VerifyUpgradeMonsterPortrait(player, (MonsterId)packet.UnitId);
        S_SetUpgradeButton buttonPacket = new() { UnitId = packet.UnitId, Cost = cost };
        player.Session.Send(buttonPacket);
    }
    
    public void HandleChangeResource(Player? player, C_ChangeResource resourcePacket)
    {
        if (player == null) return;

        S_Despawn despawnPacket = new S_Despawn();
        int objectId = resourcePacket.ObjectId;
        despawnPacket.ObjectIds.Add(objectId);
        foreach (var p in _players.Values.Where(p => p.Id != objectId)) p.Session.Send(despawnPacket);
        
        GameInfo.SheepResource += GameInfo.SheepYield;
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
        HandleInactive(gameObject, player);
    }

    private void HandleInactive(GameObject gameObject, Player player)
    {
        var type = gameObject.ObjectType;
        switch (type)
        {
            case GameObjectType.Tower:
                var tower = EnterTower((int)TowerId.Pumpkin, gameObject.PosInfo, player);
                Push(EnterGame, tower);
                break;
            
            case GameObjectType.Monster:
                if (gameObject is not Monster m) return;
                var statue = FindGameObjectById(m.StatueId);
                
                if (statue == null) return;
                var monster = EnterMonster((int)MonsterId.Tusk, statue.PosInfo, player);
                
                break;
            
            case GameObjectType.MonsterStatue:
                break;
        }
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}