using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    public struct UnitSize
    {
        public readonly UnitId UnitId;
        public readonly int SizeX;
        public readonly int SizeZ;
        
        public UnitSize(UnitId towerId, int sizeX, int sizeZ)
        {
            UnitId = towerId;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }
    }
}