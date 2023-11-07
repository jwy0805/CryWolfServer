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

            Vector3 center = GameData.FenceCenter[_storageLevel];
            Vector3 size = GameData.FenceSize[_storageLevel];
            GameData.FenceBounds = new List<Vector3>
            {
                new(center.X - size.X / 2 , 6, center.Z + size.Z / 2),
                new(center.X - size.X / 2 , 6, center.Z - size.Z / 2),
                new(center.X + size.X / 2 , 6, center.Z - size.Z / 2),
                new(center.X + size.X / 2 , 6, center.Z + size.Z / 2)
            };
            
            SpawnFence(_storageLevel);
        }
    }
    
    private void GameInit()
    {
        Stopwatch.Start();
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

    public void HandleDir(Player? player, C_Dir dirPacket)
    {
        if (player == null) return;
        GameObject? go = FindGameObjectById(dirPacket.ObjectId);
        if (go == null) return;

        GameObjectType type = go.ObjectType;
        switch (type)
        {
            case GameObjectType.Effect:
                Effect effect = (Effect)go;
                effect.Dir = dirPacket.Dir;
                effect.PacketReceived = true;
                break;
            default:
                go.Dir = dirPacket.Dir;
                break;
        }
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
                tower.Way = spawnPacket.Way;
                tower.Init();
                if (towerType is TowerId.MothLuna or TowerId.MothMoon or TowerId.MothCelestial)
                {
                    tower.CellPos = Map.FindSpawnPos(tower, SpawnWay.Any);
                    tower.StartCell = tower.CellPos;
                }
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
                monster.Way = spawnPacket.Way;
                monster.Init();
                monster.CellPos = Map.FindSpawnPos(monster, spawnPacket.Way);
                Push(EnterGame, monster);
                break;
            
            case GameObjectType.Sheep:
                Sheep sheep = ObjectManager.Instance.Add<Sheep>();
                sheep.PosInfo = spawnPacket.PosInfo;
                sheep.Info.PosInfo = sheep.PosInfo;
                sheep.Player = player;
                sheep.Init();
                sheep.CellPos = Map.FindSpawnPos(sheep, SpawnWay.Any);
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
    
    private void SpawnFence(int storageLv = 1, int fenceLv = 0)
    {
        Vector3[] fencePos = GameData.GetPos(GameData.FenceCnt[storageLv], GameData.FenceRow[storageLv], GameData.FenceStartPos[storageLv]);
        float[] fenceRotation = GameData.GetRotation(GameData.FenceCnt[storageLv], GameData.FenceRow[storageLv]);

        for (int i = 0; i < GameData.FenceCnt[storageLv]; i++)
        {
            Fence fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceName[storageLv];
            fence.CellPos = fencePos[i];
            fence.Dir = fenceRotation[i];
            fence.FenceNum = fenceLv;
            Push(EnterGame, fence);
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
        GameObject? attacker = FindGameObjectById(attackerId);
        GameObject? target = attacker?.Target;
        if (attacker == null) return;
        
        GameObjectType type = attacker.ObjectType;
        if (target == null)
        {
            switch (type)
            {
                case GameObjectType.Monster:
                case GameObjectType.Tower:
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                    break;
                
                case GameObjectType.Effect:
                    attacker.Parent!.Mp += attacker.Parent.Stat.MpRecovery;
                    if (FindGameObjectById(attackerId) is not Effect eAttacker) return;
                    eAttacker.SetEffectEffect();
                    break;
            }

            return;
        }
        if (target.Targetable == false) return;
        
        switch (attackPacket.AttackMethod)
        {
            case AttackMethod.NoAttack:
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNormalAttackEffect(target);
                }
                break;
            case AttackMethod.NormalAttack:
                int damage = attacker.TotalAttack;
                target.OnDamaged(attacker, damage);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                    cAttacker.Mp += cAttacker.Stat.MpRecovery;
                    cAttacker.SetNormalAttackEffect(target);
                }
                else if (type is GameObjectType.Projectile)
                {
                    attacker.Parent!.Mp += attacker.Parent.Stat.MpRecovery;
                    Projectile? pAttacker = FindGameObjectById(attackerId) as Projectile;
                    pAttacker?.SetProjectileEffect(target);
                }
                break;
            
            case AttackMethod.EffectAttack:
                if (!Enum.IsDefined(typeof(EffectId), attackPacket.Effect)) return;
                Effect effect = ObjectManager.Instance.CreateEffect(attackPacket.Effect);
                effect.Room = this;
                effect.Parent = attacker;
                effect.Info.Name = attackPacket.Effect.ToString();
                effect.EffectId = attackPacket.Effect;
                effect.PosInfo = effect.SetEffectPos(attacker);
                effect.Info.PosInfo = effect.PosInfo;
                effect.Init();
                Push(EnterGame_Parent, effect, effect.Parent);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                }
                break;
            
            case AttackMethod.ProjectileAttack:
                if (!Enum.IsDefined(typeof(ProjectileId), attackPacket.Projectile)) return;
                Projectile projectile = ObjectManager.Instance.CreateProjectile(attackPacket.Projectile);
                projectile.Room = this;
                projectile.PosInfo = attacker.PosInfo;
                // projectile.PosInfo.PosY = attacker.PosInfo.PosY + attacker.Stat.SizeY;
                projectile.Info.PosInfo = projectile.PosInfo;
                projectile.Info.Name = attackPacket.Projectile.ToString();
                projectile.ProjectileId = attackPacket.Projectile;
                projectile.Target = target;
                projectile.Parent = attacker;
                projectile.TotalAttack = attacker.TotalAttack;
                projectile.Init();
                Push(EnterGame, projectile);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                }
                break;
        }
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
        foreach (var p in _players.Values)
        {
            if (p.Id != objId) p.Session.Send(despawnPacket);
        }
        
        player.Resource += resourcePacket.Resource;
        player.Session.Send(new S_ChangeResource 
            { PlayerId = player.Id, Resource = player.Resource });
    }
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
    

}