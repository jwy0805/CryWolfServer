using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.etc;
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
                var towerId = (TowerId)spawnPacket.Num;
                DataManager.TowerDict.TryGetValue((int)towerId, out var towerData);
                var tBehaviour = (Behavior)Enum.Parse(typeof(Behavior), towerData!.unitBehavior);
                if (tBehaviour == Behavior.Offence)
                {
                    var towerStatue = ObjectManager.Instance.CreateTowerStatue();
                    towerStatue.PosInfo = spawnPacket.PosInfo;
                    towerStatue.Info.PosInfo = towerStatue.PosInfo;
                    towerStatue.TowerNum = spawnPacket.Num;
                    towerStatue.Player = player;
                    towerStatue.TowerId = (TowerId)spawnPacket.Num;
                    towerStatue.Room = this;
                    towerStatue.Way = towerStatue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                    towerStatue.Dir = towerStatue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
                    towerStatue.Init();
                    RegisterTowerStatue(towerStatue);
                    Push(EnterGame, towerStatue);
                }
                else if (tBehaviour == Behavior.Defence)
                {
                    var tower = EnterTower(spawnPacket.Num, spawnPacket.PosInfo, player);
                    if (spawnPacket.Register) RegisterTower(tower);
                    Push(EnterGame, tower);
                }
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                var monsterId = (MonsterId)spawnPacket.Num;
                DataManager.TowerDict.TryGetValue((int)monsterId, out var monsterData);
                var mBehaviour = (Behavior)Enum.Parse(typeof(Behavior), monsterData!.unitBehavior);
                if (mBehaviour == Behavior.Offence)
                {
                    MonsterStatue monsterStatue = ObjectManager.Instance.CreateMonsterStatue();
                    monsterStatue.PosInfo = spawnPacket.PosInfo;
                    monsterStatue.Info.PosInfo = monsterStatue.PosInfo;
                    monsterStatue.MonsterNum = spawnPacket.Num;
                    monsterStatue.Player = player;
                    monsterStatue.MonsterId = (MonsterId)spawnPacket.Num;
                    monsterStatue.Room = this;
                    monsterStatue.Way = monsterStatue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                    monsterStatue.Dir = monsterStatue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
                    monsterStatue.Init();
                    RegisterMonsterStatue(monsterStatue);
                    Push(EnterGame, monsterStatue);
                }
                else if (mBehaviour == Behavior.Defence)
                {
                    var monster = EnterMonster(spawnPacket.Num, spawnPacket.PosInfo, player);
                    monster.Init();
                    Push(EnterGame, monster);
                }
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
        
        bool canUpgrade = false;
        
        if (canUpgrade == false)
        {
            // Client에 메시지 전달 -> cost 부족
        }
        else
        {
            var go = FindGameObjectById(upgradePacket.ObjectId);
            if (go == null) return;
            int id = go.Id;
            GameObjectType type = go.ObjectType;
            PositionInfo posInfo = new() { PosX = go.PosInfo.PosX, PosY = go.PosInfo.PosY, PosZ = go.PosInfo.PosZ };
            Behavior behaviour = go.Behavior;
            LeaveGame(id);
            Broadcast(new S_Despawn { ObjectIds = { id }});
            if (behaviour == Behavior.Defence)
            {
                if (type == GameObjectType.Monster)
                {
                    if (go is not Monster m) return;
                    Monster monster = ObjectManager.Instance.CreateMonster((MonsterId)(m.MonsterNum + 1));
                    monster.PosInfo = posInfo;
                }
                else if (type == GameObjectType.Tower)
                {
                    if (go is not Tower t) return;
                    TowerId towerId = (TowerId)(t.TowerNum + 1);
                    Tower tower = ObjectManager.Instance.CreateTower(towerId);
                    tower.PosInfo = posInfo;
                    tower.Info.PosInfo = tower.PosInfo;
                    tower.TowerNum = (int)towerId;
                    tower.Player = player;
                    tower.TowerId = towerId;
                    tower.Room = this;
                    tower.Way = tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                    tower.Init();
                    Push(EnterGame, tower);
                }
            }
            else if (behaviour == Behavior.Offence)
            {
                if (type == GameObjectType.MonsterStatue)
                {
                    if (go is not MonsterStatue monsterStatue) return;
                    
                }
                else if (type == GameObjectType.TowerStatue)
                {
                    if (go is not TowerStatue towerStatue) return;
                }
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
        
        if (gameObject.ObjectType is GameObjectType.Tower or GameObjectType.TowerStatue)
        {
            TowerSlot? slotToBeDeleted = null;
            if (gameObject.Way == SpawnWay.North)
            {
                slotToBeDeleted = _northTowers.FirstOrDefault(slot => slot.ObjectId == objectId);
            }
            else if (gameObject.Way == SpawnWay.South)
            {
                slotToBeDeleted = _southTowers.FirstOrDefault(slot => slot.ObjectId == objectId);
            }
            
        }
        else if (gameObject.ObjectType is GameObjectType.Monster or GameObjectType.MonsterStatue)
        {
            MonsterSlot? slotToBeDeleted = null;
            if (gameObject.Way == SpawnWay.North)
            {
                slotToBeDeleted = _northMonsters.FirstOrDefault(slot => slot.ObjectId == objectId);
            }
            else if (gameObject.Way == SpawnWay.South)
            {
                slotToBeDeleted = _southMonsters.FirstOrDefault(slot => slot.ObjectId == objectId);
            }
        }
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}