using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Player : GameObject
{
    public CharacterId CharacterId { get; set; }
    public int AssetId { get; set; }
    public readonly HashSet<Skill> SkillUpgradedList = new() { Skill.NoSkill };
    public readonly SkillSubject SkillSubject = new();
    public readonly HashSet<int> Portraits = new ();
    public Faction Faction { get; set; }
    
    public ClientSession? Session { get; set; }

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
}