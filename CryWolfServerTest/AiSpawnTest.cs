using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace CryWolfServerTest;

[TestFixture]
public class AiSpawnTest
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
        _room.Npc = _wolfPlayer;
        _room.Push(_room.EnterGame, _sheepPlayer);
        _room.Push(_room.EnterGame, _wolfPlayer);
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -4f, PosY = 6, PosZ = 12 });
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -2.5f, PosY = 6, PosZ = 12 });
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -5.5f, PosY = 6, PosZ = 12 });
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -8f, PosY = 6, PosZ = 12 });
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -7f, PosY = 6, PosZ = 12 });
        _room.Push(_room.SpawnStatueForTest, UnitId.Wolf, new PositionInfo { PosX = -6f, PosY = 6, PosZ = 12 });
        _room.Flush();
    }

    [Test]
    public void SpawnTowerTest()
    {
        var vector1 = _room.SampleTowerPos(UnitId.Bloom);
        var vector2 = _room.SampleTowerPos(UnitId.Shell);
        var vector3 = _room.SampleTowerPos(UnitId.Shell);
        Console.WriteLine($"{vector1}, {vector2}, {vector3}");
    }
}