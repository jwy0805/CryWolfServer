using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Player : GameObject
{
    public int PlayerId { get; set; }
    public CharacterId CharacterId { get; set; }
    public int AssetId { get; set; }
    public HashSet<Skill> SkillUpgradedList = new() { Skill.NoSkill };
    public SkillSubject SkillSubject = new();
    public HashSet<int> Portraits = new ();
    public Camp Camp { get; set; }
    
    public ClientSession? Session { get; set; }

    public Player()
    {
        ObjectType = GameObjectType.Player;
        
        DataManager.ObjectDict.TryGetValue(PlayerId, out var playerData);
        if (playerData != null)
        {
            Stat.MergeFrom(playerData.stat);
            Stat.MoveSpeed = playerData.stat.MoveSpeed;
        }
    }

    public override void Init()
    {
        
    }
    
    public void OnLeaveGame()
    {
        
    }
}