using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class Player : GameObject
{
    public int PlayerNo;
    public List<Skill> SkillUpgradedList = new();
    public SkillSubject SkillSubject = new();
    
    public ClientSession Session { get; set; }

    public Player()
    {
        ObjectType = GameObjectType.Player;
        
        DataManager.ObjectDict.TryGetValue(PlayerNo, out var playerData);
        Stat.MergeFrom(playerData!.stat);
        Stat.MoveSpeed = playerData.stat.MoveSpeed;
    }

    public override void Init()
    {
        
    }
    
    public void OnLeaveGame()
    {
        
    }
}