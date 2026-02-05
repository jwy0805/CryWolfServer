using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;

namespace CryWolfServerTest;

public class SinglePlayGameGenerationTest
{
    private CancellationTokenSource _cts;
    private Task _gameLogicTask;
    
    [OneTimeSetUp]
    public void SetUp()
    {
        DataManager.LoadData();
        _cts = new CancellationTokenSource();
        _gameLogicTask = Task.Run(async () =>
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                GameLogic.Instance.Update();
                await Task.Delay(10, token);
            }
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _cts.Cancel();
        try
        {
            await _gameLogicTask;
        }
        catch (TaskCanceledException)
        {
            
        }
    }

    [Test]
    public async Task CreateSingleGame()
    {
        var session = SessionManager.Instance.Generate();
        var packet = new SinglePlayStartPacketRequired
        {
            UserId = 1,
            UserFaction = Faction.Wolf,
            UnitIds = new []
            {
                UnitId.DogBowwow, UnitId.MosquitoStinger, UnitId.PoisonBomb, UnitId.Werewolf, UnitId.SnakeNaga, UnitId.SkeletonMage
            },
            CharacterId = (int)CharacterId.Ama,
            AssetId = 1001,
            EnemyUnitIds = new []
            {
                UnitId.Hare, UnitId.FlowerPot, UnitId.Toadstool, UnitId.Blossom, UnitId.MothCelestial, UnitId.SunfloraPixie
            },
            EnemyCharacterId = (int)CharacterId.Elin,
            EnemyAssetId = 901,
            MapId = 1,
            SessionId = session.SessionId,
        };

        session.UserId = packet.UserId;
        
        // var task = await NetworkManager.Instance.StartSingleGameAsync(packet).WaitAsync(TimeSpan.FromSeconds(1));
        // Assert.That(task, Is.True);
    }
}