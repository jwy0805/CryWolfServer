using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game;

public class Storage : GameObject
{
    private readonly int[] _zCoordinates = { 0, -22, -24, -24 };
    
    public int Level { get; private set; } = 1;
    
    public Storage()
    {
        ObjectType = GameObjectType.Storage;
    }

    public void LevelUp()
    {
        if (Room == null) return;
        
        Level++;
        if (Level > 3)
        {
            Level = 3;
            return;
        }

        Room.GameInfo.NorthMaxTower += 4;
        
        if (Level == 2)
        {
            Room.GameInfo.SheepYield += 20;
        }
        else if (Level == 3)
        {
            Room.GameInfo.SheepYield += 40;
        }
        
        var packet = new S_BaseUpgrade
        {
            Faction = Faction.Sheep,
            Level = Level,
            BaseZ = _zCoordinates[Level],
        };
        
        Room.ChangeFences(Level);
        Room.Broadcast(packet);
    }
}