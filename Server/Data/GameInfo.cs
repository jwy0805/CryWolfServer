using System.Collections.Concurrent;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data;

public class GameInfo // 한 판마다 초기화되는 정보
{
    private readonly Dictionary<int, Player> _players;
    private int _mapId;
    private int _northMaxTower = 7;
    private int _northTower;
    private int _southMaxTower = 7;
    private int _southTower;
    private int _northMaxMonster = 7;
    private int _northMonster;
    private int _southMaxMonster = 7;
    private int _southMonster;
    private int _sheepResource;
    private int _wolfResource;

    public int SheepYieldUpgradeCost { get; set; } = 260;
    public int SheepYield { get; set; } = 100;
    public float SheepYieldParam { get; set; } = 1;
    public int SheepUpkeep { get; set; } = 0;
    public int TotalSheepYield => (int)Math.Round(SheepYield * SheepYieldParam);
    public int WolfYieldUpgradeCost { get; set; } = 260;
    public int WolfYield { get; set; } = 100;
    public float WolfYieldParam { get; set; } = 1;
    public int WolfUpkeep { get; set; } = 0;
    public int TotalWolfYield => (int)Math.Round(WolfYield * WolfYieldParam);
    public int WolfYieldKillFence => TotalWolfYield;
    public int WolfYieldKillSheep => TotalWolfYield * 6;
    public int NorthFenceCnt { get; set; }
    public int SouthFenceCnt { get; set; }
    public int NorthMaxFenceCnt { get; set; }
    public int SouthMaxFenceCnt { get; set; } = 8;
    public Vector3 FenceStartPos { get; set; }
    public int TheNumberOfDestroyedFence { get; set; }
    public int TheNumberOfDestroyedStatue { get; set; }
    public int TheNumberOfDestroyedSheep { get; set; }

    #region AI

    public int FenceDamageThisRound { get; set; } = 0;
    public int SheepDamageThisRound { get; set; } = 0;
    public int SheepDeathsThisRound { get; set; } = 0;
    public bool FenceMovedThisRound { get; set; } = false;
    public bool UrgentSpawn { get; set; } = false;

    #endregion
    
    private Vector3 _fenceCenter;
    public Vector3 FenceCenter 
    { 
        get => _fenceCenter;
        set
        {
            _fenceCenter = value;
            UpdateBounds();
        }
    }
        
    private Vector3 _fenceSize;
    public Vector3 FenceSize
    {
        get => _fenceSize;
        set
        {
            _fenceSize = value;
            UpdateBounds();
        }
    }
    
    public Vector3[] FenceBounds { get; private set; }
    public Vector3[] SheepBounds { get; private set; }
    
    public int StorageLevelUpCost => 300;
    public int PortalLevelUpCost => 300;
    public int SheepCount { get; set; } = 0;
    public int EnchantUpCost { get; set; }= 150;
    
    public int NorthMaxTower
    {
        get => _northMaxTower;
        set
        {
            _northMaxTower = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Sheep))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMaxTower, Max = true});
        }
    }

    public int NorthTower
    {
        get => _northTower;
        set
        {
            _northTower = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Sheep))
            {
                player.Session?.Send(new S_SetTextUI
                    { TextUI = CommonTexts.NorthCapacityText, Value = _northTower, Max = false });
            }        
        }
    }

    public int SouthMaxTower
    {
        get => _southMaxTower;
        set
        {
            _southMaxTower = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Sheep))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMaxTower, Max = true});
        }
    }

    public int SouthTower
    {
        get => _southTower;
        set
        {
            _southTower = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Sheep))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southTower, Max = false});
        }
    }
    
    public int NorthMaxMonster 
    { 
        get => _northMaxMonster;
        set
        {
            _northMaxMonster = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Wolf))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMaxMonster, Max = true});
        } 
    }

    public int NorthMonster
    {
        get => _northMonster;
        set
        {
            _northMonster = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Wolf))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMonster, Max = false});      
        }
    }

    public int SouthMaxMonster
    {
        get => _southMaxMonster;
        set
        {
            _southMaxMonster = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Wolf))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMaxMonster, Max = true});
        }
    }

    public int SouthMonster
    {
        get => _southMonster;
        set
        {
            _southMonster = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Wolf))
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMonster, Max = false});
        }
    } 

    public int SheepResource
    {
        get => _sheepResource;
        set
        {
            var plus = _sheepResource <= value;
            _sheepResource = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Sheep))
            {
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = _sheepResource });
                if (plus)
                {
                    player.Session?.Send(new S_PlaySound { Sound = Sounds.GetGold, SoundType = SoundType.D2 });   
                }
            }
        }
    }

    public int WolfResource
    {
        get => _wolfResource;
        set
        {
            var plus = _wolfResource <= value;
            _wolfResource = value;
            foreach (var player in _players.Values.Where(player => player.Faction == Faction.Wolf))
            {
                player.Session?.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = _wolfResource });
                if (plus)
                {
                    player.Session?.Send(new S_PlaySound { Sound = Sounds.GetDna, SoundType = SoundType.D2 });   
                }
            }
        }
    }
    
    public GameInfo(Dictionary<int, Player> players, int mapId)
    {
        _players = players;
        _mapId = mapId;
        NorthMaxFenceCnt = 12;
    }
    
    private void UpdateBounds()
    {
        FenceBounds = new[]
        {
            new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2),
            new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
            new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
            new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2)
        };
            
        SheepBounds = new[]
        {
            new Vector3(FenceCenter.X - FenceSize.X / 2 + 2 , 6, FenceCenter.Z + FenceSize.Z / 2 - 2),
            new Vector3(FenceCenter.X - FenceSize.X / 2 + 2, 6, FenceCenter.Z - FenceSize.Z / 2 + 2),
            new Vector3(FenceCenter.X + FenceSize.X / 2 - 2, 6, FenceCenter.Z - FenceSize.Z / 2 + 2),
            new Vector3(FenceCenter.X + FenceSize.X / 2 - 2, 6, FenceCenter.Z + FenceSize.Z / 2 - 2)
        };
    }

    public float GetSpawnRangeMinZ(GameRoom? room, Faction faction)
    {
        if (room == null) return 0;
        return faction == Faction.Sheep ? room.GameData.MinZ : FenceBounds.Max(v => v.Z) + 2;
    }

    public float GetSpawnRangeMaxZ(GameRoom? room, Faction faction)
    {
        if (room == null) return 0;
        return faction == Faction.Sheep ? FenceBounds.Max(v => v.Z) + 3 : room.GameData.MaxZ;
    }
}