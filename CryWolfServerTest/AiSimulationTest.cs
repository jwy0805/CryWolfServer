using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using Server.Game.AI;

namespace CryWolfServerTest;

[TestFixture]
public class AiSimulationTest
{
    [OneTimeSetUp]
    public void SetUp()
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ai_test.log");
            var sw = new StreamWriter(logDir) { AutoFlush = true };
            Console.SetOut(sw);
        }
        catch (Exception e)
        {
            TestContext.Progress.WriteLine(e);
            throw;
        }
        
        DataManager.LoadData();

        AiActions.OnSpawnUnit += (i, id) => TestContext.Progress.WriteLine($"AiPlayer {i} has spawned {id}");
        AiActions.OnUpgradeSkill += (i, skill) => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded skill {skill}");
        AiActions.OnUpgradeUnit += (i, id) => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded {id}");
        AiActions.OnRepairFence += i => TestContext.Progress.WriteLine($"AiPlayer {i} has repaired fence");
        AiActions.OnRepairStatue += i => TestContext.Progress.WriteLine($"AiPlayer {i} has repaired statue");
        AiActions.OnRepairPortal += i => TestContext.Progress.WriteLine($"AiPlayer {i} has repaired portal");
        AiActions.OnUpgradeStorage += i => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded storage");
        AiActions.OnUpgradePortal += i => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded portal");
        AiActions.OnUpgradeEnchant += i => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded enchant");
        AiActions.OnSpawnSheep += i => TestContext.Progress.WriteLine($"AiPlayer {i} has spawned sheep");
        AiActions.OnUpgradeYield += i => TestContext.Progress.WriteLine($"AiPlayer {i} has upgraded yield");
        
        TestContext.Progress.WriteLine("AI Simulation Test Setup Complete");
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        AiActions.OnSpawnUnit = null;
        AiActions.OnUpgradeSkill = null;
        AiActions.OnUpgradeUnit = null;
        AiActions.OnRepairFence = null;
        AiActions.OnRepairStatue = null;
        AiActions.OnRepairPortal = null;
        AiActions.OnUpgradeStorage = null;
        AiActions.OnUpgradePortal = null;
        AiActions.OnUpgradeEnchant = null;
        AiActions.OnSpawnSheep = null;
        AiActions.OnUpgradeYield = null;
    }
    
    [Test]
    public async Task RunAiGame()
    {
        Console.WriteLine($"Starting AI Simulation {DateTime.Now}");
        var room = GameLogic.Instance.CreateGameRoom(1);
        room.GameMode = GameMode.AiTest;
        NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Sheep, CharacterId.Elin, 901);
        NetworkManager.Instance.CreateNpcForAiGame(room, Faction.Wolf, CharacterId.Ama, 1001);
        
        room.RoomActivated = true;
        room.Flush();
        
        var finished = await Update(() => room.RoomActivated == false, 10, TimeSpan.FromMinutes(15));
        
        Assert.That(finished, Is.True, "Ai simulation did not finish in time");
    }

    private static async Task<bool> Update(Func<bool> condition, int ticsMs, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.IsCancellationRequested)
        {
            GameLogic.Instance.Update();
            if (condition()) return true;
            await Task.Delay(ticsMs, cts.Token);
        }

        return false;
    }
} 