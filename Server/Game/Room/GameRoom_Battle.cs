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
            if (_storageLevel > 3) _storageLevel = 3;
            GameData.StorageLevel = _storageLevel;
            
            // 울타리 생성
            if (_storageLevel != 1 && _fences.Count > 0)
            {
                foreach (var fenceId in _fences.Keys) Push(LeaveGame, fenceId);
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
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket)
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;
        
        switch (type)
        {
            case GameObjectType.Tower:
                if (!Enum.IsDefined(typeof(TowerId), spawnPacket.Num)) return;
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
                MonsterStatue monsterStatue = EnterMonsterStatue(spawnPacket.Num, spawnPacket.PosInfo, player);
                RegisterMonsterStatue(monsterStatue);
                Push(EnterGame, monsterStatue);
                break;
            
            case GameObjectType.Sheep:
                Sheep sheep = ObjectManager.Instance.Add<Sheep>();
                sheep.PosInfo = spawnPacket.PosInfo;
                sheep.Info.PosInfo = sheep.PosInfo;
                sheep.Player = player;
                sheep.Init();
                sheep.CellPos = Map.FindSpawnPos(sheep);
                Push(EnterGame, sheep);
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

    public void HandleSkillUpgrade(Player? player, C_SkillUpgrade upgradePacket)
    {
        if (player == null) return;
        
        Skill skill = upgradePacket.Skill;
        bool canUpgrade = CanUpgradeSkill(player, skill);
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> "cost가 부족합니다"
            return;
        }
        
        if (Enum.IsDefined(typeof(Skill), skill.ToString()))
            player.SkillSubject.SkillUpgraded(skill);
        else ProcessingBaseSkill(player);
        
        player.SkillUpgradedList.Add(skill);
        player.Session.Send(new S_SkillUpgrade { Skill = upgradePacket.Skill });
    }

    public void HandlePortraitUpgrade(Player? player, C_PortraitUpgrade upgradePacket)
    {
        if (player == null) return;

        bool canUpgrade = false;
        TowerId towerId = TowerId.UnknownTower;
        MonsterId monsterId = MonsterId.UnknownMonster;
        if (upgradePacket.MonsterId == MonsterId.UnknownMonster)
        {
            towerId = upgradePacket.TowerId;
            canUpgrade = CanUpgradeTower(player, towerId);
        }
        else
        {
            monsterId = upgradePacket.MonsterId;
            canUpgrade = CanUpgradeMonster(player, monsterId);
        }
        
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> cost 부족
        }
        else
        {
            if (monsterId == MonsterId.UnknownMonster) towerId = (TowerId)((int)towerId + 1);
            else monsterId = (MonsterId)((int)monsterId + 1);
            player.Session.Send(new S_PortraitUpgrade { TowerId = towerId, MonsterId = monsterId });
        }
    }

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
    {
        if (player == null) return;
        
        bool canUpgrade = true;
        
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> cost 부족
        }
        else
        {
            var go = FindGameObjectById(upgradePacket.ObjectId);
            if (go == null) return;
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
            }
        }
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
        
        switch (gameObject.ObjectType)
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
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}