using Google.Protobuf.Protocol;
using Server.Game.Object.etc;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    private struct TowerSlot 
    {
        public readonly TowerId TowerId;
        public readonly SpawnWay Way;
        public readonly int ObjectId;
        public readonly TowerStatue? Statue;
        
        public TowerSlot(TowerId towerId, SpawnWay way, int objectId = 0, TowerStatue? statue = null)
        {
            TowerId = towerId;
            Way = way;
            ObjectId = objectId;
            Statue = statue;
        }
    }

    private struct MonsterSlot
    {
        public readonly MonsterId MonsterId;
        public readonly SpawnWay Way;
        public readonly int ObjectId;
        public readonly MonsterStatue? Statue;
        
        public MonsterSlot(MonsterId monsterId, SpawnWay way, int objectId, MonsterStatue? statue)
        {
            MonsterId = monsterId;
            Way = way;
            ObjectId = objectId;
            Statue = statue;
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