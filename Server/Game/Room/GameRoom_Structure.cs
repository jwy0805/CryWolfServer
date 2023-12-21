using Google.Protobuf.Protocol;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    private struct TowerSlot
    {
        public readonly int ObjectId;
        public readonly TowerId TowerId;
        public readonly SpawnWay Way;
        
        public TowerSlot(int objectId, TowerId towerId, SpawnWay way)
        {
            ObjectId = objectId;
            TowerId = towerId;
            Way = way;
        }
    }

    private struct MonsterSlot
    {
        public readonly MonsterStatue Statue;
        public readonly MonsterId MonsterId;
        public readonly SpawnWay Way;
        
        public MonsterSlot(MonsterStatue statue, MonsterId monsterId, SpawnWay way)
        {
            Statue = statue;
            MonsterId = monsterId;
            Way = way;
        }
    }

    public struct TowerSize
    {
        public readonly TowerId TowerId;
        public readonly int SizeX;
        public readonly int SizeZ;
        
        public TowerSize(TowerId towerId, int sizeX, int sizeZ)
        {
            TowerId = towerId;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }
    }

    public struct MonsterSize
    {
        public readonly MonsterId MonsterId;
        public readonly int SizeX;
        public readonly int SizeZ;
        
        public MonsterSize(MonsterId monsterId, int sizeX, int sizeZ)
        {
            MonsterId = monsterId;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }
    }
}