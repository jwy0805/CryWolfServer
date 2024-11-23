using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom
{
    private record struct TowerSlot 
    {
        public UnitId TowerId;
        public PositionInfo PosInfo;
        public SpawnWay Way;
        public int ObjectId;
        
        public TowerSlot(UnitId unitId, PositionInfo positionInfo, SpawnWay way, int objectId = 0)
        {
            TowerId = unitId;
            PosInfo = positionInfo;
            Way = way;
            ObjectId = objectId;
        }
    }

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