using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data;

public class GameInfo
{
    private readonly Dictionary<int, Player> _players;
    private int _northMaxTower = 5;
    private int _northTower = 1;
    private int _southMaxTower = 5;
    private int _southTower = 1;
    private int _maxSheep = 5;
    private int _sheep = 1;
    private int _northMaxMonster = 6;
    private int _northMonster = 1;
    private int _southMaxMonster = 6;
    private int _southMonster = 1;
    private int _sheepResource = 200;
    private int _wolfResource = 250;
    
    public GameInfo(Dictionary<int, Player> players)
    {
        _players = players;
    }
    
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

    public int Sheep
    {
        get => _sheep;
        set
        {
            _sheep = value;
            foreach (var player in _players.Values.Where(player => player.Camp == Camp.Sheep))
                player.Session.Send(new S_SetTextUI { TextUI = CommonTexts.SubResourceText, Value = _sheep, Max = false});
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

    public int NorthFenceCnt { get; set; } = 6;
    public int SouthFenceCnt { get; set; } = 6;

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
    public int SheepYield { get; set; } = 20;

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