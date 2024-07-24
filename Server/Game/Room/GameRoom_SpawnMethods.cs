using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.etc;

namespace Server.Game;

public partial class GameRoom 
{
    private void RegisterTower(Tower tower)
    {
        PositionInfo posInfo = new PositionInfo
        {
            PosX = tower.PosInfo.PosX,
            PosY = tower.PosInfo.PosY,
            PosZ = tower.PosInfo.PosZ,
            State = State.Idle
        };
        TowerSlot towerSlot = new(tower.UnitId, posInfo, tower.Way, tower.Id);
        
        if (towerSlot.Way == SpawnWay.North)
        {
            _northTowers.Add(towerSlot);
            GameInfo.NorthTower++;
        }
        else
        {
            _southTowers.Add(towerSlot);
            GameInfo.SouthTower++;
        }
        
        S_RegisterInSlot registerPacket = new()
        {
            ObjectId = towerSlot.ObjectId,
            UnitId = (int)towerSlot.TowerId,
            ObjectType = GameObjectType.Tower,
            Way = towerSlot.Way
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(registerPacket);
    }
    
    private void RegisterMonsterStatue(MonsterStatue statue)
    {
        MonsterSlot monsterSlot = new(statue.UnitId, statue.Way, statue);

        if (monsterSlot.Way == SpawnWay.North)
        {
            _northMonsters.Add(monsterSlot);
            GameInfo.NorthMonster++;
        }
        else
        {
            _southMonsters.Add(monsterSlot);
            GameInfo.SouthMonster++;
        }
        
        S_RegisterInSlot registerPacket = new()
        {
            ObjectId = monsterSlot.Statue.Id,
            UnitId = (int)monsterSlot.MonsterId,
            ObjectType = GameObjectType.MonsterStatue,
            Way = monsterSlot.Way
        };
        _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)?.Session.Send(registerPacket);
    }

    private void UpgradeTower(Tower oldTower, Tower newTower)
    {
        TowerSlot newTowerSlot = new(newTower.UnitId, newTower.PosInfo, newTower.Way, newTower.Id);

        if (newTower.Way == SpawnWay.North)
        {
            int index = _northTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            if (index != -1)
            {
                _northTowers[index] = newTowerSlot;
            }
            else
            {
                int index2 = _southTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
                if (index2 == -1)
                {
                    var warningMsg = "(이미 죽었습니다)다시 시도해주세요.";
                    S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
                    _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(warningPacket);
                    return;
                }

                _southTowers[index2] = newTowerSlot;
            }
        }
        else
        {
            int index = _southTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
            if (index != -1)
            {
                _southTowers[index] = newTowerSlot;
            }
            else
            {
                int index2 = _northTowers.FindIndex(slot => slot.ObjectId == oldTower.Id);
                if (index2 == -1)
                {
                    var warningMsg = "(이미 죽었습니다)다시 시도해주세요.";
                    S_SendWarningInGame warningPacket = new() { Warning = warningMsg };
                    _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)?.Session.Send(warningPacket);
                    return;
                }
                _northTowers[index2] = newTowerSlot;
            }
        }
    }
    
    private void UpgradeMonsterStatue(MonsterStatue oldStatue, MonsterStatue newStatue)
    {
        MonsterSlot newMonsterSlot = new(newStatue.UnitId, newStatue.Way, newStatue);

        if (newStatue.Way == SpawnWay.North)
        {
            int index = _northMonsters.FindIndex(slot => slot.Statue.Id == oldStatue.Id);
            _northMonsters[index] = newMonsterSlot;
        }
        else
        {
            int index = _southMonsters.FindIndex(slot => slot.Statue.Id == oldStatue.Id);
            _southMonsters[index] = newMonsterSlot;
        }
    }
    
    private void SpawnFence(int storageLv = 1, int fenceLv = 0)
    {
        Vector3[] fencePos = GameData.GetPos(
            GameData.NorthFenceMax + GameData.SouthFenceMax, GameData.NorthFenceMax, GameInfo.FenceStartPos);

        for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
        {
            var fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceNames[storageLv];
            fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
            fence.Room = this;
            fence.Way = fence.CellPos.Z > GameInfo.FenceCenter.Z ? SpawnWay.North : SpawnWay.South;
            fence.CellPos = fencePos[i];
            fence.Dir = fence.Way == SpawnWay.North ? 0 : 180;
            if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
            else GameInfo.SouthFenceCnt++;
            fence.FenceNum = fenceLv;
            Push(EnterGame, fence);
        }
    }

    private Fence SpawnFence(Vector3 cellPos, int fenceLv = 0)
    {
        var storageLv = GameInfo.StorageLevel;
        var fence = ObjectManager.Instance.Add<Fence>();
        fence.Init();
        fence.Info.Name = GameData.FenceNames[storageLv];
        fence.Player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Sheep)!;
        fence.Room = this;
        fence.Way = fence.CellPos.Z > GameInfo.FenceCenter.Z ? SpawnWay.North : SpawnWay.South;
        fence.CellPos = cellPos;
        fence.Dir = fence.Way == SpawnWay.North ? 0 : 180;
        if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
        else GameInfo.SouthFenceCnt++;
        fence.FenceNum = fenceLv;
        Push(EnterGame, fence);
        
        return fence;
    }

    private void SpawnMonstersInNewRound()
    {
        var slots = _northMonsters.Concat(_southMonsters).ToList();
        
        foreach (var slot in slots)
        {
            var player = _players.Values.FirstOrDefault(p => p.Camp == Camp.Wolf)!;
            var monster = EnterMonster((int)slot.MonsterId, FindMonsterSpawnPos(slot.Statue), player);
            monster.StatueId = slot.Statue.Id;
            Push(EnterGame, monster);
            player.Session?.Send(new S_RegisterInSlot { ObjectId = monster.Id, UnitId = (int)monster.UnitId });
        }
    }

    private void SpawnTowersInNewRound()
    {
        // YieldCoin
        foreach (var sheep in _sheeps.Values)
        {
            if (sheep.YieldStop == false)
            {
                sheep.YieldCoin(GameInfo.SheepYield + sheep.YieldIncrement - sheep.YieldDecrement);
                sheep.YieldIncrement = 0;
                sheep.YieldDecrement = 0;
            }
            
            sheep.YieldStop = false;
        }
        
        // Reset State
        foreach (var tower in _towers.Values)
        {
            tower.RoundInit();
        }
    }

    private Tower EnterTower(int unitId, PositionInfo posInfo, Player player)
    {
        var tower = ObjectManager.Instance.Create<Tower>((UnitId)unitId);
        tower.PosInfo = posInfo;
        tower.Info.PosInfo = tower.PosInfo;
        tower.Player = player;
        tower.UnitId = (UnitId)unitId;
        tower.Room = this;
        tower.Way = MapId == 1 ? SpawnWay.North : tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tower.Dir = tower.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tower.Init();
        return tower;
    }

    private Monster EnterMonster(int unitId, PositionInfo posInfo, Player player)
    {
        var monster = ObjectManager.Instance.Create<Monster>((UnitId)unitId);
        monster.PosInfo = posInfo;
        monster.Info.PosInfo = monster.PosInfo;
        monster.Player = player;
        monster.UnitId = (UnitId)unitId;
        monster.Room = this;
        monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        monster.Dir = monster.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        monster.Init();
        return monster;
    }
    
    private MonsterStatue EnterMonsterStatue(int unitId, PositionInfo posInfo, Player player)
    {
        var statue = ObjectManager.Instance.Add<MonsterStatue>();
        statue.PosInfo = posInfo;
        statue.Info.PosInfo = statue.PosInfo;
        statue.Player = player;
        statue.UnitId = (UnitId)unitId;
        statue.Room = this;
        statue.Way = statue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        statue.Dir = statue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        statue.Init();
        return statue;
    }

    
    
    public Monster SpawnMonster(UnitId unitId, PositionInfo posInfo, Player player)
    {
        var monster = ObjectManager.Instance.Create<Monster>(unitId);
        monster.PosInfo = posInfo;
        monster.Info.PosInfo = monster.PosInfo;
        monster.Player = player;
        monster.UnitId = unitId;
        monster.Room = this;
        monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        monster.Dir = monster.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        monster.Init();
        Push(EnterGame, monster);
        return monster;
    }

    public void SpawnEffect(EffectId effectId, GameObject? parent, 
        PositionInfo? effectPos = null, bool trailing = false, int duration = 2000)
    {
        if (Enum.IsDefined(typeof(EffectId), effectId) == false) return;
        if (parent == null) return;
        
        var effect = ObjectManager.Instance.Create<Effect>(effectId);
        effectPos ??= parent.PosInfo;
        var position = new PositionInfo
        {   
            Dir = effectPos.Dir, 
            PosX = effectPos.PosX, 
            PosY = effectPos.PosY + 0.05f, 
            PosZ = effectPos.PosZ
        };
        var parentCopied = FindGameObjectById(parent.Id);
        if (parentCopied == null) return;
        effect.Room = this;
        effect.PosInfo = position;
        effect.Info.PosInfo = effect.PosInfo;
        effect.Info.Name = effectId.ToString();
        effect.EffectId = effectId;
        effect.Parent = parentCopied;
        effect.Duration = duration;
        effect.Init();
        EnterGameEffect(effect, parentCopied.Id, trailing, duration);
    }
    
    public Projectile SpawnProjectile(ProjectileId projectileId, GameObject? parent, float speed)
    {
        if (Enum.IsDefined(typeof(ProjectileId), projectileId) == false) return new Projectile();
        if (parent?.Target == null) return new Projectile();
        
        var projectile = ObjectManager.Instance.Create<Projectile>(projectileId);
        var position = new PositionInfo
        {   
            Dir = parent.PosInfo.Dir, 
            PosX = parent.PosInfo.PosX, 
            PosY = parent.PosInfo.PosY + parent.Stat.SizeY, 
            PosZ = parent.PosInfo.PosZ
        };
        
        projectile.Room = this;
        projectile.Parent = parent;
        projectile.Target = parent.Target;
        projectile.PosInfo = position;
        projectile.Info.PosInfo = projectile.PosInfo;
        projectile.Info.Name = projectileId.ToString();
        projectile.ProjectileId = projectileId;
        projectile.DestPos = parent.Target.CellPos;
        projectile.Attack = parent.TotalAttack;
        projectile.MoveSpeed = speed;
        projectile.Init();
        EnterGameProjectile(projectile, projectile.DestPos, speed, parent.Id);
        return projectile;
    }   
    
    public Projectile SpawnProjectile(
        ProjectileId projectileId, GameObject? parent, PositionInfo posInfo, float speed, GameObject target)
    {
        if (Enum.IsDefined(typeof(ProjectileId), projectileId) == false) return new Projectile();
        if (parent?.Target == null) return new Projectile();
        
        var projectile = ObjectManager.Instance.Create<Projectile>(projectileId);
        var position = new PositionInfo
        {   
            Dir = posInfo.Dir, 
            PosX = posInfo.PosX, 
            PosY = posInfo.PosY + parent.Stat.SizeY, 
            PosZ = posInfo.PosZ
        };
        
        projectile.Room = this;
        projectile.Parent = parent;
        projectile.Target = target;
        projectile.PosInfo = position;
        projectile.Info.PosInfo = projectile.PosInfo;
        projectile.Info.Name = projectileId.ToString();
        projectile.ProjectileId = projectileId;
        projectile.DestPos = target.CellPos;
        projectile.Attack = parent.TotalAttack;
        projectile.MoveSpeed = speed;
        projectile.Init();
        EnterGameProjectile(projectile, projectile.DestPos, speed, parent.Id);
        return projectile;
    }
    
    private Sheep EnterSheep(Player player)
    {
        Sheep sheep = ObjectManager.Instance.Add<Sheep>();
        sheep.PosInfo = new PositionInfo { State = State.Idle };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Room = this;
        sheep.Player = player;
        sheep.Init();
        sheep.CellPos = Map.FindSheepSpawnPos(sheep);

        return sheep;
    }

    public void EnterSheepByServer(Player player)
    {
        var sheep = ObjectManager.Instance.Add<Sheep>();
        var sheepCellPos = Map.FindSheepSpawnPos(sheep);
        
        sheep.PosInfo = new PositionInfo
        {
            State = State.Idle, PosX = sheepCellPos.X, PosY = sheepCellPos.Y, PosZ = sheepCellPos.Z
        };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Room = this;
        sheep.Player = player;
        sheep.Init();
        sheep.CellPos = new Vector3(sheep.PosInfo.PosX, sheep.PosInfo.PosY, sheep.PosInfo.PosZ);
        Push(EnterGame, sheep);
        GameInfo.SheepCount++;
    }
}