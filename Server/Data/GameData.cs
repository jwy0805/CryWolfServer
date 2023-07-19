using System.Numerics;

namespace Server.Data;

public class GameData
{
    // Game 초기 설정
    // 가변
    public static int[] SpawnMonsterCnt = { 1, 1, 0 }; // { West, North, East }
    
    // 불변
    public static Vector3 Center = new Vector3(0.0f, 6.0f, 0.0f); // Center of the Map
    
    public static int SpawnersCnt = 3;
    public static Vector3[] SpawnerPos { get; set; } = {
        new Vector3(-40.0f, 6.0f, 0.0f), // West
        new Vector3(0.0f, 6.0f, 40.0f),  // North
        new Vector3(40.0f, 6.0f, 0.0f)  // East
    };
    
    public static float RoundTime = 30.0f;

    #region MapData

    public static List<Vector3> WestMap = new()
    {
        new Vector3(-62, 0, -16),
        new Vector3(-19, 0, -16),
        new Vector3(-19, 0, 20),
        new Vector3(-62, 0, 20)
    };

    public static List<Vector3> SouthMap = new()
    {
        new Vector3(-13, 0, -16),
        new Vector3(-13, 0, -21),
        new Vector3(13, 0, -21),
        new Vector3(13, 0, -16)
    };

    public static List<Vector3> EastMap = new()
    {
        new Vector3(13, 0, 10),
        new Vector3(13, 0, -16),
        new Vector3(70, 0, -16),
        new Vector3(70, 0, 10),
    };

    public static List<Vector3> NorthMap = new()
    {
        new Vector3(-19, 0, 50),
        new Vector3(-19, 0, 20),
        new Vector3(15, 0, 20),
        new Vector3(15, 0, 50)
    };
    
    public List<Vector3> MidMap = new()
    {
        new Vector3(-19, 0, 20),
        new Vector3(-19, 0, -16),
        new Vector3(19, 0, -16),
        new Vector3(19, 0, 20)
    };

    #endregion
    
    #region FenceData

    public static string[] FenceName = { "", "FenceLv1", "FenceLv2", "FenceLv3" };
    public static int[] FenceCnt = { 0, 18, 22, 28 };
    public static int CurrentFenceCnt = 0;
    public static int[] FenceRow = { 0, 4, 5, 6 };

    public static Vector3[] FenceStartPos =
    {
        new Vector3(0, 0, 0), new Vector3(-3, 6, -7),
        new Vector3(-4, 6, -7), new Vector3(-5, 6, -8)
    };

    public static Vector3[] FenceCenter =
    {
        new Vector3(0, 0, 0), new Vector3(0, 6, -2),
        new Vector3(0, 6, -1), new Vector3(0, 6, 0)
    };

    public static Vector3[] FenceSize =
    {
        new Vector3(0, 0, 0), new Vector3(8, 6, 10),
        new Vector3(10, 6, 12), new Vector3(12, 6, 16)
    };

    public static List<Vector3> FenceBounds;
    
    public static List<Vector3>[] NorthFenceBounds =
    {
        new List<Vector3>(),
        new List<Vector3>
        {
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2, 6, FenceCenter[1].Z + FenceSize[1].Z / 2 + 1),  
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2, 6, FenceCenter[1].Z + FenceSize[1].Z / 2 - 1),  
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2, 6, FenceCenter[1].Z + FenceSize[1].Z / 2 - 1),  
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2, 6, FenceCenter[1].Z + FenceSize[1].Z / 2 + 1)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2, 6, FenceCenter[2].Z + FenceSize[2].Z / 2 + 1.5f),  
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2, 6, FenceCenter[2].Z + FenceSize[2].Z / 2 - 1.5f),  
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2, 6, FenceCenter[2].Z + FenceSize[2].Z / 2 - 1.5f),  
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2, 6, FenceCenter[2].Z + FenceSize[2].Z / 2 + 1.5f)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2 + 2),  
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2 - 2),  
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2 - 2),  
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2 + 2)  
        },
    };
    
    public static List<Vector3>[] WestFenceBounds =
    {
        new List<Vector3>(),
        new List<Vector3>
        {
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2 - 1, 6, FenceCenter[1].Z + FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2 - 1, 6, FenceCenter[1].Z - FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2 + 1, 6, FenceCenter[1].Z - FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X - FenceSize[1].X / 2 + 1, 6, FenceCenter[1].Z + FenceSize[1].Z / 2)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z + FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z - FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2 + 1.5f, 6, FenceCenter[2].Z - FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X - FenceSize[2].X / 2 + 1.5f, 6, FenceCenter[2].Z + FenceSize[2].Z / 2)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z - FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2 + 2, 6, FenceCenter[3].Z - FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X - FenceSize[3].X / 2 + 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2)  
        },
    };
    
    public static List<Vector3>[] EastFenceBounds =
    {
        new List<Vector3>(),
        new List<Vector3>
        {
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2 - 1, 6, FenceCenter[1].Z + FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2 - 1, 6, FenceCenter[1].Z - FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2 + 1, 6, FenceCenter[1].Z - FenceSize[1].Z / 2),  
          new Vector3(FenceCenter[1].X + FenceSize[1].X / 2 + 1, 6, FenceCenter[1].Z + FenceSize[1].Z / 2)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z + FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z - FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z - FenceSize[2].Z / 2),  
          new Vector3(FenceCenter[2].X + FenceSize[2].X / 2 - 1.5f, 6, FenceCenter[2].Z + FenceSize[2].Z / 2)  
        },
        new List<Vector3>
        {
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z - FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z - FenceSize[3].Z / 2),  
          new Vector3(FenceCenter[3].X + FenceSize[3].X / 2 - 2, 6, FenceCenter[3].Z + FenceSize[3].Z / 2)  
        },
    };
    
    // public static Bounds NorthBounds;
    // public static Bounds WestBounds;
    // public static Bounds EastBounds;
    //
    public static List<Vector3>[] SheepBounds =
    {
        new List<Vector3>(),
        new List<Vector3>()
        {
            new Vector3(Center.X - 1.5f, Center.Y, Center.Z + 1),
            new Vector3(Center.X - 1.5f, Center.Y, Center.Z - 1),
            new Vector3(Center.X + 1.5f, Center.Y, Center.Z - 1),
            new Vector3(Center.X + 1.5f, Center.Y, Center.Z + 1),
        },
        new List<Vector3>()
        {
            new Vector3(-2.5f, Center.Y, 5.5f),
            new Vector3(-2.5f, Center.Y, -1.5f),
            new Vector3(2.5f, Center.Y, -1.5f),
            new Vector3(2.5f, Center.Y, 5.5f),
        },
        new List<Vector3>()
        {
            new Vector3(-5, Center.Y, 7),
            new Vector3(-5, Center.Y, -1),
            new Vector3(5, Center.Y, -1),
            new Vector3(5, Center.Y, 7),
        },
    };
        
    public static Vector3[] GetPos(int cnt, int row, Vector3 startPos)
    {
        Vector3[] posArr = new Vector3[cnt];
        int col = cnt / 2 - row;

        for (int i = 0; i < row; i++)
        {
            posArr[i] = new Vector3(startPos.X + (i * 2), startPos.Y, startPos.Z); // south fence
            posArr[row + col + i] = new Vector3(startPos.X + (i * 2), startPos.Y, startPos.Z + 2 * col); // north fence
        }

        for (int i = 0; i < col; i++)
        {
            posArr[row + i] = new Vector3(row , startPos.Y, startPos.Z + 1 + 2 * i); // east fence
            posArr[row + col + row + i] =
                new Vector3(-row, startPos.Y, startPos.Z + 1 + 2 * i); // west fence
        }

        return posArr;
    }
    
    public static float[] GetRotation(int cnt, int row)
    {
        float[] rotationArr = new float[cnt];
        int col = cnt / 2 - row;

        for (int i = 0; i < row; i++)
        {
            rotationArr[i] = -90;
            rotationArr[row + col + i] = 90;
        }

        for (int i = 0; i < col; i++)
        {
            rotationArr[row + i] = 180;
            rotationArr[row + col + row + i] = 0;
        }

        return rotationArr;
    }

    #endregion

    // 게임 진행 정보
    #region GameInfo

    public static int StartSheepResource = 5000;
    public static int SheepYield = 20;
    public static int StartWolfResource = 5000;
    
    public static int StorageLevel = 0;
    public static int[] StorageLvUpCost = { 0, 600, 2000 };
    public static int TowerCapacity = 0;
    public static int[] TowerMaxCapacity = {0, 3, 5, 7};
    public static int SheepCapacity = 3;
    public static int[] SheepMaxCapacity = {0, 6, 12, 20};
    

    public static int[] MonsterCapacity = new int[3];
    public static int MonsterMaxCapacity = 0;
    
    #endregion
    
    public static readonly Dictionary<string, string> Tower = new Dictionary<string, string>()
    {
        { "00", "Bud" }, { "01", "Bloom" }, { "02", "Blossom" },
        { "10", "PracticeDummy" }, { "11", "TargetDummy" }, { "12", "TrainingDummy" },
        { "20", "SunBlossom" }, { "21", "SunflowerFairy" }, { "22", "SunfloraPixie" },
        { "30", "MothLuna" }, { "31", "MothMoon" }, { "32", "MothCelestial" },
        { "40", "Soul" }, { "41", "Haunt" }, { "42", "SoulMage" },
    };
    
    public static readonly Dictionary<string, string> Monster = new Dictionary<string, string>()
    {
        { "50", "WolfPup" }, { "51", "Wolf" }, { "52", "Werewolf" },
        { "60", "Lurker" }, { "61", "Creeper" }, { "62", "Horror" },
        { "70", "Snakelet" }, { "71", "Snake" }, { "72", "SnakeNaga" },
        { "80", "MosquitoBug" }, { "81", "MosquitoPester" }, { "82", "MosquitoStinger" },
        { "90", "Shell" }, { "91", "Spike" }, { "92", "Hermit" },
    };
    
    public static readonly List<string> TowerList = new List<string>(Tower.Values);

    public static readonly List<string> MonsterList = new List<string>(Monster.Values);

    public static readonly List<string> UnitList = TowerList.Concat(MonsterList).ToList();

    // public static readonly Dictionary<Define.Skill, int> SkillCost = new Dictionary<Define.Skill, int>()
    // {
    //     { Define.Skill.Bloom3Combo, 120 }
    // };
    //
    // public static readonly Dictionary<Define.UnitId, int> UnitUpgradeCost = new Dictionary<Define.UnitId, int>()
    // {
    //     { Define.UnitId.Bud, 160 }, { Define.UnitId.Bloom, 430 },
    //     { Define.UnitId.PracticeDummy, 200 }, { Define.UnitId.TargetDummy, 520 },
    //     { Define.UnitId.SunBlossom, 250 }, { Define.UnitId.SunflowerFairy, 630 },
    //     { Define.UnitId.MothLuna, 220 }, { Define.UnitId.MothMoon, 500 },
    //     { Define.UnitId.Soul, 180 }, { Define.UnitId.Haunt, 640 },
    //     { Define.UnitId.WolfPup, 150 }, { Define.UnitId.Wolf, 700 },
    //     { Define.UnitId.Lurker, 240 }, { Define.UnitId.Creeper, 480 },
    //     { Define.UnitId.Snakelet, 300 }, { Define.UnitId.Snake, 660 },
    //     { Define.UnitId.MosquitoBug, 180 }, { Define.UnitId.MosquitoPester, 450 },
    //     { Define.UnitId.Shell, 250 }, { Define.UnitId.Spike, 650 },
    // };
    public static readonly Dictionary<string, string[]> SkillTree = new Dictionary<string, string[]>()
    {
        { "BloomAttack", new[] { "free" } },
        { "BloomAttackSpeed", new[] { "free" } },
        { "BloomRange", new[] { "free" } },
        { "BloomAttackSpeed2", new[] { "BloomAttackSpeed" } },
        { "BloomAirAttack", new[] { "BloomRange" } },
        { "Bloom3Combo", new[] { "BloomAttack", "BloomAttackSpeed2" } },

        { "BlossomPoison", new[] { "free" } },
        { "BlossomAccuracy", new[] { "free" } },
        { "BlossomAttack", new[] { "BlossomPoison", "BlossomAccuracy" } },
        { "BlossomAttackSpeed", new[] { "BlossomPoison", "BlossomAccuracy" } },
        { "BlossomRange", new[] { "BlossomPoison", "BlossomAccuracy" } },
        { "BlossomDeath", new[] { "BlossomAttack", "BlossomAttackSpeed", "BlossomRange" } },

        { "BudAttack", new[] { "free" } },
        { "BudAttackSpeed", new[] { "free" } },
        { "BudRange", new[] { "free" } },
        { "BudSeed", new[] { "BudAttack", "BudAttackSpeed" } },
        { "BudDouble", new[] { "BudSeed", "BudRange" } },

        { "HauntLongAttack", new[] { "free" } },
        { "HauntAttackSpeed", new[] { "free" } },
        { "HauntAttack", new[] { "free" } },
        { "HauntPoisonResist", new[] { "HauntLongAttack", "HauntAttackSpeed", "HauntAttack" } },
        { "HauntFireResist", new[] { "HauntLongAttack", "HauntAttackSpeed", "HauntAttack" } },
        { "HauntFire", new[] { "HauntPoisonResist", "HauntFireResist" } },

        { "MothCelestialSheepHealth", new[] { "free" } },
        { "MothCelestialGroundAttack", new[] { "free" } },
        { "MothCelestialAccuracy", new[] { "free" } },
        { "MothCelestialFireResist", new[] { "MothCelestialSheepHealth", "MothCelestialGroundAttack" } },
        { "MothCelestialPoisonResist", new[] { "MothCelestialSheepHealth", "MothCelestialGroundAttack" } },
        { "MothCelestialPoison", new[] { "MothCelestialAccuracy" } },
        { "MothCelestialBreedSheep", new[] { "MothCelestialPoisonResist", "MothCelestialFireResist" } },

        { "MothMoonRemoveDebuffSheep", new[] { "free" } },
        { "MothMoonHealSheep", new[] { "free" } },
        { "MothMoonRange", new[] { "free" } },
        { "MothMoonOutput", new[] { "MothMoonRemoveDebuffSheep", "MothMoonHealSheep" } },
        { "MothMoonAttackSpeed", new[] { "MothMoonRange" } },

        { "MothLunaAttack", new[] { "free" } },
        { "MothLunaSpeed", new[] { "free" } },
        { "MothLunaAccuracy", new[] { "free" } },
        { "MothLunaFaint", new[] { "MothLunaAttack", "MothLunaSpeed" } },

        { "PracticeDummyHealth", new[] { "free" } },
        { "PracticeDummyDefence", new[] { "free" } },
        { "PracticeDummyHealth2", new[] { "PracticeDummyHealth" } },
        { "PracticeDummyDefence2", new[] { "PracticeDummyDefence" } },
        { "PracticeDummyAggro", new[] { "PracticeDummyHealth2", "PracticeDummyDefence2" } },

        { "SoulMageAvoid", new[] { "free" } },
        { "SoulMageDefenceAll", new[] { "free" } },
        { "SoulMageFireDamage", new[] { "free" } },
        { "SoulMageShareDamage", new[] { "SoulMageDefenceAll" } },
        { "SoulMageTornado", new[] { "SoulMageFireDamage" } },
        { "SoulMageDebuffResist", new[] { "SoulMageAvoid", "SoulMageShareDamage", "SoulMageTornado" } },
        { "SoulMageNatureAttack", new[] { "SoulMageAvoid", "SoulMageShareDamage", "SoulMageTornado" } },
        { "SoulMageCritical", new[] { "SoulMageDebuffResist", "SoulMageNatureAttack" } },

        { "SoulAttack", new[] { "free" } },
        { "SoulDefence", new[] { "free" } },
        { "SoulHealth", new[] { "free" } },
        { "SoulDrain", new[] { "SoulAttack", "SoulDefence", "SoulHealth" } },

        { "SunBlossomHealth", new[] { "free" } },
        { "SunBlossomSlow", new[] { "free" } },
        { "SunBlossomHeal", new[] { "SunBlossomHealth" } },
        { "SunBlossomSlowAttack", new[] { "SunBlossomSlow" } },

        { "SunfloraPixieFaint", new[] { "free" } },
        { "SunfloraPixieHeal", new[] { "free" } },
        { "SunfloraPixieRange", new[] { "free" } },
        { "SunfloraPixieCurse", new[] { "SunfloraPixieFaint" } },
        { "SunfloraPixieAttackSpeed", new[] { "SunfloraPixieHeal" } },
        { "SunfloraPixieTriple", new[] { "SunfloraPixieRange" } },
        {
            "SunfloraPixieDebuffRemove",
            new[] { "SunfloraPixieCurse", "SunfloraPixieAttackSpeed" }
        },
        {
            "SunfloraPixieAttack",
            new[] { "SunfloraPixieCurse", "SunfloraPixieAttackSpeed" }
        },
        { "SunfloraPixieInvincible", new[] { "SunfloraPixieDebuffRemove", "SunfloraPixieAttack" } },

        { "SunflowerFairyAttack", new[] { "free" } },
        { "SunflowerFairyDouble", new[] { "free" } },
        { "SunflowerFairyDefence", new[] { "SunflowerFairyAttack" } },
        { "SunflowerFairyMpDown", new[] { "SunflowerFairyDouble" } },
        { "SunflowerFairyFenceHeal", new[] { "SunflowerFairyDefence", "SunflowerFairyMpDown" } },

        { "TargetDummyHealth", new[] { "free" } },
        { "TargetDummyHeal", new[] { "free" } },
        { "TargetDummyFireResist", new[] { "TargetDummyHealth", "TargetDummyHeal" } },
        { "TargetDummyPoisonResist", new[] { "TargetDummyHealth", "TargetDummyHeal" } },
        { "TargetDummyReflection", new[] { "TargetDummyHealth", "TargetDummyHeal" } },

        { "TrainingDummyAggro", new[] { "free" } },
        { "TrainingDummyHeal", new[] { "free" } },
        { "TrainingDummyFaint", new[] { "free" } },
        { "TrainingDummyHealth", new[] { "TrainingDummyAggro", "TrainingDummyHeal" } },
        { "TrainingDummyDefence", new[] { "TrainingDummyAggro", "TrainingDummyHeal" } },
        { "TrainingDummyPoisonResist", new[] { "TrainingDummyAggro", "TrainingDummyHeal" } },
        { "TrainingDummyFireResist", new[] { "TrainingDummyAggro", "TrainingDummyHeal" } },
    };
    
    public static List<string> SkillUpgradedList = new List<string>();
}









