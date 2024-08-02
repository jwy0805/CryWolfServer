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
        _sheepPlayer.Camp = Camp.Sheep;
        _wolfPlayer.Camp = Camp.Wolf;
    }

    [Test]
    public void GenerateGameRoomTest()
    {
        var room = GameLogic.Instance.CreateGameRoom(1);
        room.Push(room.EnterGame, _sheepPlayer);
        room.Push(room.EnterGame, _wolfPlayer);
        room.Flush();
        
        var sheepPlayer = room.FindPlayer(player => player is Player { Camp: Camp.Sheep });
        var wolfPlayer = room.FindPlayer(player => player is Player { Camp: Camp.Wolf });
        
        Console.WriteLine($"{room.RoomId}, {sheepPlayer!.Id}, {wolfPlayer!.Id}");
        Assert.Pass();
    }
}