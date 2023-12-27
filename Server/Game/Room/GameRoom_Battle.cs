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
                TowerId towerType = (TowerId)spawnPacket.Num;
                Tower tower = ObjectManager.Instance.CreateTower(towerType);
                tower.PosInfo = spawnPacket.PosInfo;
                tower.Info.PosInfo = tower.PosInfo;
                tower.TowerNum = spawnPacket.Num;
                tower.Player = player;
                tower.TowerId = towerType;
                tower.Room = this;
                tower.Way = tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                tower.Init();
                if (spawnPacket.Register) RegisterTower(tower);
                Push(EnterGame, tower);
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                MonsterId monsterType = (MonsterId)spawnPacket.Num;
                Monster monster = ObjectManager.Instance.CreateMonster(monsterType);
                monster.PosInfo = spawnPacket.PosInfo;
                monster.Info.PosInfo = monster.PosInfo;
                monster.MonsterNum = spawnPacket.Num;
                monster.Player = player;
                monster.MonsterId = monsterType;
                monster.Room = this;
                monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                monster.Init();
                Push(EnterGame, monster);
                break;
            
            case GameObjectType.MonsterStatue:
                if (!Enum.IsDefined(typeof(MonsterId), spawnPacket.Num)) return;
                MonsterStatue statue = ObjectManager.Instance.CreateMonsterStatue();
                statue.PosInfo = spawnPacket.PosInfo;
                statue.Info.PosInfo = statue.PosInfo;
                statue.MonsterNum = spawnPacket.Num;
                statue.Player = player;
                statue.MonsterId = (MonsterId)spawnPacket.Num;
                statue.Room = this;
                statue.Way = statue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
                statue.Dir = statue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
                statue.Init();
                RegisterMonsterStatue(statue);
                Push(EnterGame, statue);
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

    private void RegisterTower(Tower tower) 
    {
        TowerSlot towerSlot = new(tower.Id, tower.TowerId, tower.Way);
        int slotNum = 0;
        if (towerSlot.Way == SpawnWay.North)
        {
            _northTowers.Add(towerSlot);
            slotNum = _northTowers.Count - 1;
        }
        else
        {
            _southTowers.Add(towerSlot);
            slotNum = _southTowers.Count - 1;
        }
        
        S_RegisterTower registerPacket = new()
        {
            TowerId = (int)towerSlot.TowerId,
            ObjectId = towerSlot.ObjectId,
            Way = towerSlot.Way,
            SlotNumber = slotNum
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(registerPacket);
    }
    
    private void RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue, statue.MonsterId, statue.Way);
        int slotNum = 0;
        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
            slotNum = _northMonsters.Count - 1;
        }
        else
        {
            _southMonsters.Add(monsterSlot);
            slotNum = _southMonsters.Count - 1;
        }
        
        S_RegisterMonster registerPacket = new()
        {
            MonsterId = (int)monsterSlot.MonsterId,
            ObjectId = monsterSlot.Statue.Id,
            Way = monsterSlot.Way,
            SlotNumber = slotNum
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)?.Session.Send(registerPacket);
    }
    
    private void SpawnFence(int storageLv = 1, int fenceLv = 0)
    {
        Vector3[] fencePos = GameData.GetPos(GameData.NorthFenceMax + GameData.SouthFenceMax, 8, GameData.FenceStartPos);
        float[] fenceRotation = GameData.GetRotation(GameData.NorthFenceMax + GameData.SouthFenceMax, 8);

        for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
        {
            Fence fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceName[storageLv];
            fence.CellPos = fencePos[i];
            fence.Dir = fenceRotation[i];
            fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            fence.Room = this;
            fence.FenceNum = fenceLv;
            Push(EnterGame, fence);
        }
    }

    private void SpawnMonster()
    {
        List<MonsterSlot> slots = _northMonsters.Concat(_southMonsters).ToList();
        foreach (var slot in slots)
        {
            Monster monster = ObjectManager.Instance.CreateMonster(slot.MonsterId);
            monster.MonsterNum = slot.Statue.MonsterNum;
            monster.PosInfo = FindMonsterSpawnPos(slot.Statue);
            monster.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)!;
            monster.MonsterId = slot.MonsterId;
            monster.Way = slot.Way;
            monster.Room = this;
            monster.Init();
            monster.CellPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
            Push(EnterGame, monster);
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

    public void HandleUnitUpgrade(Player? player, C_UnitUpgrade upgradePacket)
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
            player.Session.Send(new S_UnitUpgrade { TowerId = towerId, MonsterId = monsterId });
        }
    }

    public void HandleChangeResource(Player? player, C_ChangeResource resourcePacket)
    {
        if (player == null) return;

        S_Despawn despawnPacket = new S_Despawn();
        int objId = resourcePacket.ObjectId;
        despawnPacket.ObjectIds.Add(objId);
        foreach (var p in _players.Values.Where(p => p.Id != objId)) p.Session.Send(despawnPacket);
        
        GameInfo.SheepResource += GameInfo.SheepYield;
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}