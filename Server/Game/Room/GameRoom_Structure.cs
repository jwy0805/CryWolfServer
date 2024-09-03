using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom : JobSerializer
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

    private record struct MonsterSlot
    {
        public readonly UnitId MonsterId;
        public readonly SpawnWay Way;
        public readonly MonsterStatue Statue;
        
        public MonsterSlot(UnitId unitId, SpawnWay way, MonsterStatue statue)
        {
            MonsterId = unitId;
            Way = way;
            Statue = statue;
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