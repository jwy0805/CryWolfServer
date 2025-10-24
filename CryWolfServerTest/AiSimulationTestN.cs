using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace CryWolfServerTest;

[TestFixture]
public class AiSimulationTestN
{
    [OneTimeSetUp]
    public void SetUp()
    {
        DataManager.LoadData();
    }

    [Test]
    public async Task RunAiGame()
    {
        const int roomCount = 16;
        var rooms = new List<GameRoom>(roomCount);
        
        for (int i = 0; i < roomCount; i++)
        {
            var room = GameLogic.Instance.CreateGameRoom(1);
            room.GameMode = GameMode.AiTest;
            NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Sheep, CharacterId.Elin, 901);
            NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Wolf, CharacterId.Ama, 1001);

            room.RoomActivated = true;
            room.Flush();
            rooms.Add(room);
        }

        var finished = await UpdateAll(() => rooms.All(r => r.RoomActivated == false),
            10, TimeSpan.FromMinutes(15));
        
        if (!finished)
        {
            var aliveRooms = rooms.Where(r => r.RoomActivated).Select(r => r.RoomId).ToArray();
            Assert.Fail($"Multi AI simulation did not finish in time. Alive rooms: {string.Join(",", aliveRooms)}");
        }

        Assert.That(finished, Is.True, "Multi AI simulation did not finish in time");
    }

    private static async Task<bool> UpdateAll(Func<bool> condition, int ticsMs, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.IsCancellationRequested)
        {
            GameLogic.Instance.Update();
            if (condition()) return true;

            try
            {
                await Task.Delay(ticsMs, cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return false;
    }
}