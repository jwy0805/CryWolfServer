using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace CryWolfServerTest;

[TestFixture]
public class AiSimulationTest
{
    [SetUp]
    public void SetUp()
    {
        DataManager.LoadData();
    }

    [Test]
    public void RunAiGame()
    {
        // 1) Create Room
        var room = GameLogic.Instance.CreateGameRoom(1);
        room.GameMode = GameMode.AiTest;
        
        // 2) Create AI Players
        var sheepPlayer = NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Sheep, CharacterId.Elin, 901);
        var wolfPlayer = NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Wolf, CharacterId.Ama, 1001);
        
        room.Push(room.SetAssets);
        room.Flush();

        // 3) 
    }
}