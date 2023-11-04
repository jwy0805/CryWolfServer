using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class RockPile : GameObject      
{
    private readonly int _rockPileNum = 2;
    public SpawnWay Way { get; set; } = SpawnWay.Any;
    
    public RockPile()
    {
        ObjectType = GameObjectType.RockPile;
    }

    public override void Init()
    {
        DataManager.ObjectDict.TryGetValue(_rockPileNum, out var rockPileData);
        if (rockPileData == null) throw new InvalidDataException();
        Stat.MergeFrom(rockPileData.stat);
        Stat.Hp = rockPileData.stat.MaxHp;
    }
}