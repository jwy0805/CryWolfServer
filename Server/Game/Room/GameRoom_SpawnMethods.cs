using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public partial class GameRoom 
{
    // Run when initialize game and storage upgrade.
    private void SpawnFence(int storageLv = 1, int fenceLv = 0)
    {
        Vector3[] fencePos = GameData.GetPos(
            GameData.NorthFenceMax + GameData.SouthFenceMax, GameData.NorthFenceMax, GameInfo.FenceStartPos);

        for (int i = 0; i < GameData.NorthFenceMax + GameData.SouthFenceMax; i++)
        {
            var fence = ObjectManager.Instance.Add<Fence>();
            fence.Init();
            fence.Info.Name = GameData.FenceNames[storageLv];
            fence.Player = _players.Values.FirstOrDefault(p => p.Faction == Faction.Sheep)!;
            fence.Room = this;
            fence.CellPos = fencePos[i];
            fence.Way = fence.CellPos.Z > GameInfo.FenceCenter.Z ? SpawnWay.North : SpawnWay.South;
            fence.Dir = fence.Way == SpawnWay.North ? 0 : 180;
            if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
            else GameInfo.SouthFenceCnt++;
            fence.FenceNum = fenceLv;
            Push(EnterGame, fence);
        }
    }

    // Run when the land is expanded.
    private Fence SpawnFence(Vector3 cellPos, int fenceLv = 0)
    {
        var storageLv = GameInfo.StorageLevel;
        var fence = ObjectManager.Instance.Add<Fence>();
        fence.Init();
        fence.Info.Name = GameData.FenceNames[storageLv];
        fence.Player = _players.Values.FirstOrDefault(p => p.Faction == Faction.Sheep)!;
        fence.Room = this;
        fence.CellPos = cellPos;
        fence.Way = fence.CellPos.Z > GameInfo.FenceCenter.Z ? SpawnWay.North : SpawnWay.South;
        fence.Dir = fence.Way == SpawnWay.North ? 0 : 180;
        if (fence.Way == SpawnWay.North) GameInfo.NorthFenceCnt++;
        else GameInfo.SouthFenceCnt++;
        fence.FenceNum = fenceLv;
        Push(EnterGame, fence);
        
        return fence;
    }

    private void SpawnTowersInNewRound()
    {
        // YieldCoin
        foreach (var sheep in _sheeps.Values)
        {
            if (sheep.YieldStop == false)
            {
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
    
    private void SpawnMonstersInNewRound()
    {
        foreach (var statue in _statues.Values)
        {
            var monster = SpawnMonster(statue.UnitId, FindMonsterSpawnPos(statue), statue.Player);
            monster.StatueId = statue.Id;
        }
    }
    
    private Tower SpawnTower(UnitId unitId, PositionInfo posInfo, Player player)
    {
        var tower = ObjectManager.Instance.Create<Tower>(unitId);
        tower.PosInfo = posInfo;
        tower.Info.PosInfo = tower.PosInfo;
        tower.Player = player;
        tower.UnitId = unitId;
        tower.Room = this;
        tower.AddBuffAction = AddBuff;
        tower.Way = MapId == 1 ? SpawnWay.North : tower.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        tower.Dir = tower.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        tower.Init();
        Push(EnterGame, tower);
        return tower;
    }
    
    private MonsterStatue SpawnMonsterStatue(UnitId unitId, PositionInfo posInfo, Player? player)
    {
        if (player == null) return new MonsterStatue();
        var statue = ObjectManager.Instance.Add<MonsterStatue>();
        statue.PosInfo = posInfo;
        statue.Info.PosInfo = statue.PosInfo;
        statue.Player = player;
        statue.UnitId = unitId;
        statue.Room = this;
        statue.Way = statue.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        statue.Dir = statue.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        statue.Init();
        Push(EnterGame, statue);
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
        monster.AddBuffAction = AddBuff;
        monster.Way = monster.PosInfo.PosZ > 0 ? SpawnWay.North : SpawnWay.South;
        monster.Dir = monster.Way == SpawnWay.North ? (int)Direction.N : (int)Direction.S;
        monster.Init();
        Push(EnterGame, monster);
        return monster;
    }
    
    public void SpawnEffect(EffectId effectId, GameObject? caster , GameObject? master, 
        PositionInfo? effectPos = null, bool trailing = false, int duration = 2000)
    {
        if (Enum.IsDefined(typeof(EffectId), effectId) == false) return;
        if (master == null) return;
        
        var effect = ObjectManager.Instance.Create<Effect>(effectId);
        effectPos ??= master.PosInfo;
        var position = new PositionInfo
        {   
            Dir = effectPos.Dir, 
            PosX = effectPos.PosX, 
            PosY = effectPos.PosY + 0.05f, 
            PosZ = effectPos.PosZ
        };
        
        effect.Room = this;
        effect.PosInfo = position;
        effect.Info.PosInfo = effect.PosInfo;
        effect.Info.Name = effectId.ToString();
        effect.EffectId = effectId;
        effect.Parent = caster;
        effect.Duration = duration;
        effect.Init();
        Push(EnterGameEffect, effect, master.Id, trailing, duration);
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
        Push(EnterGameProjectile, projectile, projectile.DestPos, speed, parent.Id);
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
        Push(EnterGameProjectile, projectile, projectile.DestPos, speed, parent.Id);
        return projectile;
    }

    public void SpawnPrimeSheep(SheepId sheepId, Player player)
    {
        var sheep = ObjectManager.Instance.Create<Sheep>(sheepId);
        var pos = GameData.InitFenceCenter;
        sheep.PosInfo = new PositionInfo { PosX = pos.X, PosY = pos.Y, PosZ = pos.Z, State = State.Idle };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Player = player;
        sheep.AddBuffAction = AddBuff;
        sheep.SheepId = sheepId;
        sheep.Room = this;
        sheep.Init();
        Push(EnterGame, sheep);
        GameInfo.SheepCount++;
    }

    // Enter sheep by summon button or moth celestial skill
    public void SpawnSheep(Player player)
    {
        var sheep = ObjectManager.Instance.Create<Sheep>((SheepId)(player.AssetId + 1));
        var sheepCellPos = Map.FindSheepSpawnPos(sheep);
        
        sheep.PosInfo = new PositionInfo
        {
            State = State.Idle, PosX = sheepCellPos.X, PosY = sheepCellPos.Y, PosZ = sheepCellPos.Z
        };
        sheep.Info.PosInfo = sheep.PosInfo;
        sheep.Player = player;
        sheep.AddBuffAction = AddBuff;
        sheep.SheepId = (SheepId)(player.AssetId + 1);
        sheep.Room = this;
        sheep.Init();
        sheep.CellPos = new Vector3(sheep.PosInfo.PosX, sheep.PosInfo.PosY, sheep.PosInfo.PosZ);
        Push(EnterGame, sheep);
        GameInfo.SheepCount++;
    }
}