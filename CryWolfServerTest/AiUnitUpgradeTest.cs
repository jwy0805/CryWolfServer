using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace CryWolfServerTest;

[TestFixture]
public class AiUnitUpgradeTest
{
    private readonly Player _sheepPlayer = ObjectManager.Instance.Add<Player>();
    private readonly Player _wolfPlayer = ObjectManager.Instance.Add<Player>();
    private GameRoom _room;

    [SetUp]
    public void SetUp()
    {
        DataManager.LoadData();
        
        _sheepPlayer.Faction = Faction.Sheep;
        _wolfPlayer.Faction = Faction.Wolf;

        _room = GameLogic.Instance.CreateGameRoom(1);
        _room.Push(_room.EnterGame, _sheepPlayer);
        _room.Push(_room.EnterGame, _wolfPlayer);
    }
}