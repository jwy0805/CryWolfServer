using System.Diagnostics;
using System.Numerics;
using Google.Protobuf.Protocol;

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
            {   // 기존 울타리 삭제
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
        // if (player == null) return;
        // GameObject? go = FindGameObjectById(movePacket.ObjectId);
        // if (go == null) return;
        //
        // Vector3 v = new Vector3(movePacket.PosX, movePacket.PosY, movePacket.PosZ);
        // Vector3 cellPos = Util.Util.NearestCell(v);
        // if (go.ObjectType == GameObjectType.Player) go.CellPos = cellPos;
        // else go.ApplyMap(cellPos);
    }
    
    public void HandleSpawn(Player? player, C_Spawn spawnPacket) // 클라이언트의 요청으로 Spawn되는 경우
    {
        if (player == null) return;
        GameObjectType type = spawnPacket.Type;
        
        switch (type)
        {
            case GameObjectType.Tower:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                bool lackOfTowerCost = VerifyResourceForTower(player, spawnPacket.Num);
                bool lackOfTowerCapacity = VerifyCapacityForTower(player, spawnPacket.Num, spawnPacket.Way);
                if (lackOfTowerCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
                if (lackOfTowerCapacity)
                {
                    SendWarningMessage(player, "인구수를 초과했습니다.");
                    return;
                }
                var tower = EnterTower(spawnPacket.Num, spawnPacket.PosInfo, player);
                if (spawnPacket.Register) RegisterTower(tower);
                Push(EnterGame, tower);
                break;

            case GameObjectType.Monster:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                SpawnMonster((UnitId)spawnPacket.Num, spawnPacket.PosInfo, player);
                break;
            
            case GameObjectType.MonsterStatue:
                if (!Enum.IsDefined(typeof(UnitId), spawnPacket.Num)) return;
                bool lackOfMonsterCost = VerifyResourceForMonster(player, spawnPacket.Num);
                bool lackOfMonsterCapacity = VerifyCapacityForMonster(player, spawnPacket.Num, spawnPacket.Way);
                if (lackOfMonsterCost)
                {
                    SendWarningMessage(player, "골드가 부족합니다.");
                    return;
                }
                if (lackOfMonsterCapacity)
                {
                    SendWarningMessage(player, "인구수를 초과했습니다.");
                    return;
                }
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
                var effectType = (EffectId)spawnPacket.Num;
                var effect = ObjectManager.Instance.Create<Effect>(effectType);
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

    #region Summary

    /// <summary>
    /// Handle Attack method by packets.
    /// OnDamaged method of Additional Attack must be set separately, in override method.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="attackPacket"></param>

    #endregion
    public void HandleAttack(Player? player, C_Attack attackPacket)
    {
        if (player == null) return;
        int attackerId = attackPacket.ObjectId;
        GameObject? attacker = FindGameObjectById(attackerId);
        GameObject? target = attacker?.Target;
        
        if (attacker == null) return;
        GameObjectType type = attacker.ObjectType;

        if (attackPacket.AttackMethod == AttackMethod.EffectAttack)
        {
            var effect = EnterEffect(attackPacket.Effect, attacker);
            if (effect.Parent != null) Push(EnterGameParent, effect, effect.Parent);
        }

        if (target == null || target.Targetable == false)
        {
            if (type is not (GameObjectType.Tower or GameObjectType.Monster)) return;
            Creature cAttacker = (Creature)attacker;
            cAttacker.SetNextState();
            return;
        }
        
        switch (attackPacket.AttackMethod)
        {
            case AttackMethod.NoAttack:
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.ApplyAttackEffect(target);
                }
                break;
            
            case AttackMethod.NormalAttack:
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                    cAttacker.Mp += cAttacker.MpRecovery;
                    cAttacker.ApplyAttackEffect(target);
                }
                // else if (type is GameObjectType.Projectile)
                // {
                //     attacker.Parent!.Mp += attacker.Parent.MpRecovery;
                //     Projectile? pAttacker = FindGameObjectById(attackerId) as Projectile;
                //     if (pAttacker?.Parent is Creature parent) 
                //         parent.SetProjectileEffect(target, pAttacker.ProjectileId);
                // }
                break;
            
            case AttackMethod.ProjectileAttack:
                if (!Enum.IsDefined(typeof(ProjectileId), attackPacket.Projectile)) return;
                var projectile = ObjectManager.Instance.Create<Projectile>(attackPacket.Projectile);
                projectile.Room = this;
                projectile.PosInfo = attacker.PosInfo;
                // projectile.PosInfo.PosY = attacker.PosInfo.PosY + attacker.Stat.SizeY;
                projectile.Info.PosInfo = projectile.PosInfo;
                projectile.Info.Name = attackPacket.Projectile.ToString();
                projectile.ProjectileId = attackPacket.Projectile;
                projectile.Target = target;
                projectile.Parent = attacker;
                projectile.Attack = attacker.Attack;
                projectile.Init();
                Push(EnterGame, projectile);
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.SetNextState();
                }
                break;
            
            case AttackMethod.AdditionalProjectileAttack:
                if (type is GameObjectType.Monster or GameObjectType.Tower)
                {
                    Creature cAttacker = (Creature)attacker;
                    cAttacker.ApplyAdditionalProjectileEffect(target);
                }
                break;
        }
    }
    
    public void HandleMotion(Player? player, C_Motion motionPacket)
    {
        if (player == null) return;
        if (FindGameObjectById(motionPacket.ObjectId) is not Creature go) return;
        go.SetNextState(motionPacket.State);
    }
    
    public void HandleEffectActivate(Player? player, C_EffectActivate dirPacket)
    {   // Effect 자체에 공격 등 효과가 있는 경우 Effect Controller에서 패킷 전송
        if (player == null) return;
        GameObject? go = FindGameObjectById(dirPacket.ObjectId);
        if (go == null) return;
        var effect = (Effect)go;
        effect.PacketReceived = true;
    }
    
    public void HandleSkill(Player? player, C_Skill skillPacket)
    {   // 공격이 아닌 버프, 디버프 스킬 등
        if (player == null) return;
        
        Creature? creature = FindGameObjectById(skillPacket.ObjectId) as Creature;
        creature?.RunSkill();
        creature?.SetNextState();
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
    
    public void HandleLeave(Player? player, C_Leave leavePacket)
    {
        if (player == null) return;
        LeaveGame(leavePacket.ObjectId);
    }
}