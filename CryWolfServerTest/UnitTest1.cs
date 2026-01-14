using Google.Protobuf.Protocol;
using Server.Game;
using Moq;

namespace CryWolfServerTest;

[TestFixture]
public class GenerateGameRoomByTwoPlayersTest
{
    private readonly Player _sheepPlayer = ObjectManager.Instance.Add<Player>();
    private readonly Player _wolfPlayer = ObjectManager.Instance.Add<Player>();

    [SetUp]
    public void SetUp()
    {
        _sheepPlayer.Faction = Faction.Sheep;
        _wolfPlayer.Faction = Faction.Wolf;
    }

    [Test]
    public async Task GenerateGameRoomTest()
    {
        var room = await GameLogic.Instance.CreateGameRoomAsync(1);
        room.Push(room.EnterGame, _sheepPlayer);
        room.Push(room.EnterGame, _wolfPlayer);
        room.Flush();
        
        var sheepPlayer = room.FindPlayer(player => player is Player { Faction: Faction.Sheep });
        var wolfPlayer = room.FindPlayer(player => player is Player { Faction: Faction.Wolf });
        
        Console.WriteLine($"{room.RoomId}, {sheepPlayer!.Id}, {wolfPlayer!.Id}");
        Assert.Pass();
    }
}