using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data;

public class GameInfo // 한 판마다 초기화되는 정보
{
    private readonly Dictionary<int, Player> _players;
    private int _northMaxTower = 6;
    private int _northTower = 0;
    private int _southMaxTower = 6;
    private int _southTower = 0;
    private int _maxSheep = 5;
    private int _sheepCount = 0;
    private int _northMaxMonster = 6;
    private int _northMonster = 0;
    private int _southMaxMonster = 6;
    private int _southMonster = 0;
    private int _sheepResource = 1000;
    private int _wolfResource = 250;
    public int SheepYield { get; set; } = 80;
    public int NorthFenceCnt { get; set; } = 0;
    public int SouthFenceCnt { get; set; } = 0;
    public int NorthMaxFenceCnt { get; set; } = 8;
    public int SouthMaxFenceCnt { get; set; } = 8;
    
    public GameInfo(Dictionary<int, Player> players)
    {
        _players = players;
    }
    
    public int MaxStorageLevel => 2;
    public int StorageLevel { get; set; } = 0;
    public int StorageLevelUpCost => 400;
    
    public int NorthMaxTower
    {
        get => _northMaxTower;
        set
        {
            _northMaxTower = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMaxTower, Max = true});
        }
    }

    public int NorthTower
    {
        get => _northTower;
        set
        {
            _northTower = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northTower, Max = false});
        }
    }

    public int SouthMaxTower
    {
        get => _southMaxTower;
        set
        {
            _southMaxTower = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMaxTower, Max = true});
        }
    }

    public int SouthTower
    {
        get => _southTower;
        set
        {
            _southTower = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southTower, Max = false});
        }
    }

    public int MaxSheep
    {
        get => _maxSheep;
        set
        {
            _maxSheep = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = _maxSheep, Max = true});
        }
    }

    public int SheepCount
    {
        get => _sheepCount;
        set
        {
            _sheepCount = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = _sheepCount, Max = false});
        }
    } 

    public int NorthMaxMonster 
    { 
        get => _northMaxMonster;
        set
        {
            _northMaxMonster = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Wolf))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMaxMonster, Max = true});
        } 
    }

    public int NorthMonster
    {
        get => _northMonster;
        set
        {
            _northMonster = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Wolf))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.NorthCapacityText, Value = _northMonster, Max = false});      
        }
    }

    public int SouthMaxMonster
    {
        get => _southMaxMonster;
        set
        {
            _southMaxMonster = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Wolf))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMaxMonster, Max = true});
        }
    }

    public int SouthMonster
    {
        get => _southMonster;
        set
        {
            _southMonster = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Wolf))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SouthCapacityText, Value = _southMonster, Max = false});
        }
    } 

    public int SheepResource
    {
        get => _sheepResource;
        set
        {
            _sheepResource = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = _sheepResource});
        }
    }

    public int WolfResource
    {
        get => _wolfResource;
        set
        {
            _wolfResource = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Wolf))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.ResourceText, Value = _wolfResource});
        }
    }
    
    public int WolfYield { get; set; } = 8;

    public bool NorthPortal { get; set; } = false;
    public bool SouthPortal { get; set; } = false;
}