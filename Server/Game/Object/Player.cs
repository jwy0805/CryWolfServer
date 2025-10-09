using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Player : GameObject
{
    private UnitId[] _unitIds = Array.Empty<UnitId>();
    private UnitId[] _currentUnitIds = Array.Empty<UnitId>();
    public CharacterId CharacterId { get; set; }
    public int AssetId { get; set; }
    public readonly HashSet<Skill> SkillUpgradedList = new() { Skill.NoSkill };
    public readonly SkillSubject SkillSubject = new();
    public readonly HashSet<int> Portraits = new ();
    public bool IsNpc { get; set; }
    public Faction Faction { get; set; }
    public ClientSession? Session { get; set; }
    public int WinRankPoint { get; set; }
    public int LoseRankPoint { get; set; }
    public int RankPoint { get; set; }
    public List<UnitId> AvailableUnits { get; set; }= new();
    
    public UnitId[] UnitIds
    {
        get => _unitIds;
        set
        {
            _unitIds = value;
            _currentUnitIds = value;
            AvailableUnits.AddRange(
                _unitIds.SelectMany(id =>
                {
                    var level = (int)id % 100 % 3;
                    return level switch
                    {
                        0 => new[] { id, id - 1, id - 2 },
                        1 => new[] { id },
                        2 => new[] { id, id - 1 },
                        _ => Array.Empty<UnitId>()
                    };
                })
            );
        }
    }
    
    public UnitId[] CurrentUnitIds => _currentUnitIds;

    public Player()
    {
        ObjectType = GameObjectType.Player;
    }

    public override void Init()
    {
        
    }
    
    public void OnLeaveGame()
    {
        
    }
    
    public void UpdateCurrentUnits(UnitId oldId, UnitId newId)
    {
        int idx = Array.IndexOf(_currentUnitIds, oldId);
        if (idx < 0) return;
        _currentUnitIds[idx] = newId;
    }
}