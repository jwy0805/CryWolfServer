using System.Numerics;
using Google.Protobuf.Protocol;

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
    public static int CurrentRockPileCnt = 0;
    public static int[] FenceRow = { 0, 4, 5, 6 };

    public static readonly Vector3[] FenceStartPos =
    {
        new Vector3(0, 0, 0), new Vector3(-3, 6, -7),
        new Vector3(-4, 6, -7), new Vector3(-5, 6, -8)
    };

    public static readonly Vector3[] FenceCenter =
    {
        new Vector3(0, 0, 0), new Vector3(0, 6, -2),
        new Vector3(0, 6, -1), new Vector3(0, 6, 0)
    };

    public static readonly Vector3[] FenceSize =
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
    
    public static readonly List<Vector3>[] SheepBounds =
    {
        new List<Vector3>(),
        new List<Vector3>()
        {
            new Vector3(Center.X - 2.5f, Center.Y, Center.Z + 2f),
            new Vector3(Center.X - 2.5f, Center.Y, Center.Z - 2f),
            new Vector3(Center.X + 2.5f, Center.Y, Center.Z - 2f),
            new Vector3(Center.X + 2.5f, Center.Y, Center.Z + 2f),
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
            rotationArr[i] = 0;
            rotationArr[row + col + i] = 180;
        }

        for (int i = 0; i < col; i++)
        {
            rotationArr[row + i] = 90;
            rotationArr[row + col + row + i] = -90;
        }

        return rotationArr;
    }

    #endregion

    // 게임 진행 정보
    #region GameInfo

    public static readonly float RoundTime = 13000f;

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
    
    public static readonly Dictionary<string, string> Tower = new()
    {
        { "00", "Bud" }, { "01", "Bloom" }, { "02", "Blossom" },
        { "10", "PracticeDummy" }, { "11", "TargetDummy" }, { "12", "TrainingDummy" },
        { "20", "SunBlossom" }, { "21", "SunflowerFairy" }, { "22", "SunfloraPixie" },
        { "30", "MothLuna" }, { "31", "MothMoon" }, { "32", "MothCelestial" },
        { "40", "Soul" }, { "41", "Haunt" }, { "42", "SoulMage" },
    };
    
    public static readonly Dictionary<string, string> Monster = new()
    {
        { "50", "WolfPup" }, { "51", "Wolf" }, { "52", "Werewolf" },
        { "60", "Lurker" }, { "61", "Creeper" }, { "62", "Horror" },
        { "70", "Snakelet" }, { "71", "Snake" }, { "72", "SnakeNaga" },
        { "80", "MosquitoBug" }, { "81", "MosquitoPester" }, { "82", "MosquitoStinger" },
        { "90", "Shell" }, { "91", "Spike" }, { "92", "Hermit" },
    };
    
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
    
    public static readonly Dictionary<Skill, Skill[]> SkillTree = new()
    {
        { Skill.BloomAttack, new[] { Skill.NoSkill } },
        { Skill.BloomAttackSpeed, new[] { Skill.NoSkill } },
        { Skill.BloomRange, new[] { Skill.NoSkill } },
        { Skill.BloomAttackSpeed2, new[] { Skill.BloomAttackSpeed } },
        { Skill.BloomAirAttack, new[] { Skill.BloomRange } },
        { Skill.Bloom3Combo, new[] { Skill.BloomAttack, Skill.BloomAttackSpeed2 } },
    
        { Skill.BlossomPoison, new[] { Skill.NoSkill } },
        { Skill.BlossomAccuracy, new[] { Skill.NoSkill } },
        { Skill.BlossomAttack, new[] { Skill.BlossomPoison, Skill.BlossomAccuracy } },
        { Skill.BlossomAttackSpeed, new[] { Skill.BlossomPoison, Skill.BlossomAccuracy } },
        { Skill.BlossomRange, new[] { Skill.BlossomPoison, Skill.BlossomAccuracy } },
        { Skill.BlossomDeath, new[] { Skill.BlossomAttack, Skill.BlossomAttackSpeed, Skill.BlossomRange } },
    
        { Skill.BudAttack, new[] { Skill.NoSkill } },
        { Skill.BudAttackSpeed, new[] { Skill.NoSkill } },
        { Skill.BudRange, new[] { Skill.NoSkill } },
        { Skill.BudSeed, new[] { Skill.BudAttack, Skill.BudAttackSpeed } },
        { Skill.BudDouble, new[] { Skill.BudSeed, Skill.BudRange } },
    
        { Skill.HauntLongAttack, new[] { Skill.NoSkill } },
        { Skill.HauntAttackSpeed, new[] { Skill.NoSkill } },
        { Skill.HauntAttack, new[] { Skill.NoSkill } },
        { Skill.HauntPoisonResist, new[] { Skill.HauntLongAttack, Skill.HauntAttackSpeed, Skill.HauntAttack } },
        { Skill.HauntFireResist, new[] { Skill.HauntLongAttack, Skill.HauntAttack, Skill.HauntAttack } },
        { Skill.HauntFire, new[] { Skill.HauntPoisonResist, Skill.HauntFireResist } },
    
        { Skill.MothCelestialSheepHealth, new[] { Skill.NoSkill } },
        { Skill.MothCelestialGroundAttack, new[] { Skill.NoSkill } },
        { Skill.MothCelestialAccuracy, new[] { Skill.NoSkill } },
        { Skill.MothCelestialFireResist, new[] { Skill.MothCelestialSheepHealth, Skill.MothCelestialGroundAttack } },
        { Skill.MothCelestialPoisonResist, new[] { Skill.MothCelestialSheepHealth, Skill.MothCelestialGroundAttack } },
        { Skill.MothCelestialPoison, new[] { Skill.MothCelestialAccuracy } },
        { Skill.MothCelestialBreedSheep, new[] { Skill.MothCelestialPoisonResist, Skill.MothCelestialFireResist } },
    
        { Skill.MothMoonRemoveDebuffSheep, new[] { Skill.NoSkill } },
        { Skill.MothMoonHealSheep, new[] { Skill.NoSkill } },
        { Skill.MothMoonRange, new[] { Skill.NoSkill } },
        { Skill.MothMoonOutput, new[] { Skill.MothMoonRemoveDebuffSheep, Skill.MothMoonHealSheep } },
        { Skill.MothMoonAttackSpeed, new[] { Skill.MothMoonRange } },
    
        { Skill.MothLunaAttack, new[] { Skill.NoSkill } },
        { Skill.MothLunaSpeed, new[] { Skill.NoSkill } },
        { Skill.MothLunaAccuracy, new[] { Skill.NoSkill } },
        { Skill.MothLunaFaint, new[] { Skill.MothLunaAttack, Skill.MothLunaSpeed } },
    
        { Skill.PracticeDummyHealth, new[] { Skill.NoSkill } },
        { Skill.PracticeDummyDefence, new[] { Skill.NoSkill } },
        { Skill.PracticeDummyHealth2, new[] { Skill.PracticeDummyHealth } },
        { Skill.PracticeDummyDefence2, new[] { Skill.PracticeDummyDefence } },
        { Skill.PracticeDummyAggro, new[] { Skill.PracticeDummyHealth2, Skill.PracticeDummyDefence2} },
    
        { Skill.SoulMageAvoid, new[] { Skill.NoSkill } },
        { Skill.SoulMageDefenceAll, new[] { Skill.NoSkill } },
        { Skill.SoulMageFireDamage, new[] { Skill.NoSkill } },
        { Skill.SoulMageShareDamage, new[] { Skill.SoulMageDefenceAll } },
        { Skill.SoulMageTornado, new[] { Skill.SoulMageFireDamage } },
        { Skill.SoulMageDebuffResist, new[] { Skill.SoulMageAvoid, Skill.SoulMageShareDamage, Skill.SoulMageTornado } },
        { Skill.SoulMageNatureAttack, new[] { Skill.SoulMageAvoid, Skill.SoulMageShareDamage, Skill.SoulMageTornado } },
        { Skill.SoulMageCritical, new[] { Skill.SoulMageDebuffResist, Skill.SoulMageNatureAttack } },
    
        { Skill.SoulAttack, new[] { Skill.NoSkill } },
        { Skill.SoulDefence, new[] { Skill.NoSkill } },
        { Skill.SoulHealth, new[] { Skill.NoSkill } },
        { Skill.SoulDrain, new[] { Skill.SoulAttack, Skill.SoulDefence, Skill.SoulHealth } },
    
        { Skill.SunBlossomHealth, new[] { Skill.NoSkill } },
        { Skill.SunBlossomSlow, new[] { Skill.NoSkill } },
        { Skill.SunBlossomHeal, new[] { Skill.SunBlossomHealth } },
        { Skill.SunBlossomSlowAttack, new[] { Skill.SunBlossomSlow } },
    
        { Skill.SunfloraPixieFaint, new[] { Skill.NoSkill } },
        { Skill.SunfloraPixieHeal, new[] { Skill.NoSkill } },
        { Skill.SunfloraPixieRange, new[] { Skill.NoSkill } },
        { Skill.SunfloraPixieCurse, new[] { Skill.SunfloraPixieFaint } },
        { Skill.SunfloraPixieAttackSpeed, new[] { Skill.SunfloraPixieHeal } },
        { Skill.SunfloraPixieTriple, new[] { Skill.SunfloraPixieRange } },
        {
            Skill.SunfloraPixieDebuffRemove,
            new[] { Skill.SunfloraPixieCurse, Skill.SunfloraPixieAttackSpeed }
        },
        {
            Skill.SunfloraPixieAttack,
            new[] { Skill.SunfloraPixieCurse, Skill.SunfloraPixieAttackSpeed }
        },
        { Skill.SunfloraPixieInvincible, new[] { Skill.SunfloraPixieDebuffRemove, Skill.SunfloraPixieAttack } },
    
        { Skill.SunflowerFairyAttack, new[] { Skill.NoSkill } },
        { Skill.SunflowerFairyDouble, new[] { Skill.NoSkill } },
        { Skill.SunflowerFairyDefence, new[] { Skill.SunflowerFairyAttack } },
        { Skill.SunflowerFairyMpDown, new[] { Skill.SunflowerFairyDouble } },
        { Skill.SunflowerFairyFenceHeal, new[] { Skill.SunflowerFairyDefence, Skill.SunflowerFairyMpDown } },
    
        { Skill.TargetDummyHealth, new[] { Skill.NoSkill } },
        { Skill.TargetDummyHeal, new[] { Skill.NoSkill } },
        { Skill.TargetDummyFireResist, new[] { Skill.TargetDummyHeal, Skill.TargetDummyHealth } },
        { Skill.TargetDummyPoisonResist, new[] { Skill.TargetDummyHeal, Skill.TargetDummyHealth } },
        { Skill.TargetDummyReflection, new[] { Skill.TargetDummyHeal, Skill.TargetDummyHealth } },
    
        { Skill.TrainingDummyAggro, new[] { Skill.NoSkill } },
        { Skill.TrainingDummyHeal, new[] { Skill.NoSkill } },
        { Skill.TrainingDummyFaint, new[] { Skill.NoSkill } },
        { Skill.TrainingDummyHealth, new[] { Skill.TrainingDummyAggro, Skill.TrainingDummyHeal } },
        { Skill.TrainingDummyDefence, new[] { Skill.TrainingDummyAggro, Skill.TrainingDummyHeal } },
        { Skill.TrainingDummyPoisonResist, new[] { Skill.TrainingDummyAggro, Skill.TargetDummyHeal } },
        { Skill.TrainingDummyFireResist, new[] { Skill.TrainingDummyAggro, Skill.TargetDummyHeal } },
    
        { Skill.WolfPupSpeed , new [] { Skill.NoSkill }},
        { Skill.WolfPupHealth , new [] { Skill.NoSkill }},
        { Skill.WolfPupAttackSpeed , new [] { Skill.WolfPupSpeed }},
        { Skill.WolfPupAttack , new [] { Skill.WolfPupHealth }},
        
        { Skill.WolfDefence, new [] { Skill.NoSkill } },
        { Skill.WolfDrain,  new [] { Skill.NoSkill } },
        { Skill.WolfAvoid, new [] { Skill.WolfDefence } },
        { Skill.WolfCritical, new [] { Skill.WolfDrain } },
        { Skill.WolfFireResist, new [] { Skill.WolfAvoid } },
        { Skill.WolfPoisonResist, new [] { Skill.WolfAvoid } },
        { Skill.WolfDna, new [] { Skill.WolfFireResist, Skill.WolfPoisonResist, Skill.WolfCritical} },
    
        { Skill.HorrorRollPoison, new [] { Skill.NoSkill } },
        { Skill.HorrorPoisonStack, new [] { Skill.NoSkill } },
        { Skill.HorrorHealth, new [] { Skill.HorrorRollPoison, Skill.HorrorPoisonStack } },
        { Skill.HorrorPoisonResist, new [] { Skill.HorrorRollPoison, Skill.HorrorPoisonStack } },
        { Skill.HorrorDefence, new [] { Skill.HorrorRollPoison, Skill.HorrorPoisonStack } },
        { Skill.HorrorPoisonBelt, new [] { Skill.HorrorRollPoison, Skill.HorrorPoisonStack } },
    
        { Skill.SnakeletSpeed, new [] { Skill.NoSkill } },
        { Skill.SnakeletRange, new [] { Skill.NoSkill } },
        { Skill.SnakeletAttackSpeed, new [] { Skill.SnakeletSpeed } },
        { Skill.SnakeletAttack, new [] { Skill.SnakeRange } },
    
        { Skill.SnakeNagaAttack, new [] { Skill.NoSkill } },
        { Skill.SnakeNagaRange, new [] { Skill.NoSkill } },
        { Skill.SnakeNagaFireResist, new [] { Skill.SnakeNagaAttack, Skill.SnakeNagaRange } },
        { Skill.SnakeNagaCritical, new [] { Skill.SnakeNagaAttack, Skill.SnakeNagaRange } },
        { Skill.SnakeNagaDrain, new [] { Skill.SnakeNagaAttack, Skill.SnakeNagaRange } },
        { Skill.SnakeNagaMeteor, new [] { Skill.SnakeNagaFireResist, Skill.SnakeNagaCritical, Skill.SnakeNagaDrain } },
    
        { Skill.CreeperSpeed, new [] { Skill.NoSkill } },
        { Skill.CreeperAttackSpeed, new [] { Skill.NoSkill } },
        { Skill.CreeperAttack, new [] { Skill.NoSkill } },
        { Skill.CreeperRoll, new [] { Skill.CreeperSpeed, Skill.CreeperAttackSpeed, Skill.CreeperAttack } },
        { Skill.CreeperPoison, new [] { Skill.CreeperSpeed, Skill.CreeperAttackSpeed, Skill.CreeperAttack } },
    
        { Skill.MosquitoBugSpeed, new [] { Skill.NoSkill } },
        { Skill.MosquitoBugDefence, new [] { Skill.NoSkill } },
        { Skill.MosquitoBugAvoid, new [] { Skill.NoSkill } },
        { Skill.MosquitoBugWoolDown, new [] { Skill.MosquitoBugSpeed, Skill.MosquitoBugDefence, Skill.MosquitoBugAvoid } },
    
        { Skill.LurkerSpeed, new [] { Skill.NoSkill } },
        { Skill.LurkerHealth, new [] { Skill.NoSkill } },
        { Skill.LurkerDefence, new [] { Skill.NoSkill } },
        { Skill.LurkerHealth2, new [] { Skill.LurkerSpeed, Skill.LurkerHealth, Skill.LurkerDefence } },
    
        { Skill.MosquitoPesterAttack, new [] { Skill.NoSkill } },
        { Skill.MosquitoPesterHealth, new [] { Skill.NoSkill } },
        { Skill.MosquitoPesterWoolDown2, new [] { Skill.MosquitoPesterAttack } },
        { Skill.MosquitoPesterWoolRate, new [] { Skill.MosquitoPesterHealth } },
        { Skill.MosquitoPesterWoolStop, new [] { Skill.MosquitoPesterWoolDown2, Skill.MosquitoPesterWoolRate } },
    
        { Skill.SpikeSelfDefence, new [] { Skill.NoSkill } },
        { Skill.SpikeLostHeal, new [] { Skill.NoSkill } },
        { Skill.SpikeDefence, new [] { Skill.SpikeSelfDefence } },
        { Skill.SpikeAttack, new [] { Skill.SpikeLostHeal } },
        { Skill.SpikeDoubleBuff, new [] { Skill.SpikeDefence, Skill.SpikeAttack } },
    
        { Skill.WerewolfThunder, new [] { Skill.NoSkill } },
        { Skill.WerewolfDebuffResist, new [] { Skill.NoSkill } },
        { Skill.WerewolfFaint, new [] { Skill.WerewolfThunder } },
        { Skill.WerewolfHealth, new [] { Skill.WerewolfDebuffResist } },
        { Skill.WerewolfEnhance, new [] { Skill.WerewolfFaint, Skill.WerewolfHealth } },
    
        { Skill.ShellAttackSpeed, new [] { Skill.NoSkill } },
        { Skill.ShellSpeed, new [] { Skill.NoSkill } },
        { Skill.ShellHealth, new [] { Skill.NoSkill } },
        { Skill.ShellRoll, new [] { Skill.ShellAttackSpeed, Skill.ShellSpeed, Skill.ShellHealth } },
    
        { Skill.SnakeAttack, new [] { Skill.NoSkill } },
        { Skill.SnakeAttackSpeed, new [] { Skill.NoSkill } },
        { Skill.SnakeRange, new [] { Skill.NoSkill } },
        { Skill.SnakeAccuracy, new [] { Skill.SnakeAttack, Skill.SnakeAttackSpeed, Skill.SnakeRange } },
        { Skill.SnakeFire, new [] { Skill.SnakeAccuracy } },
    
        { Skill.MosquitoStingerLongAttack, new [] { Skill.NoSkill } },
        { Skill.MosquitoStingerHealth, new [] { Skill.NoSkill } },
        { Skill.MosquitoStingerAvoid, new [] { Skill.NoSkill } },
        { Skill.MosquitoStingerPoison, new [] { Skill.MosquitoStingerLongAttack } },
        { Skill.MosquitoStingerPoisonResist, new [] { Skill.MosquitoStingerHealth } },
        { Skill.MosquitoStingerInfection, new [] { Skill.MosquitoStingerAvoid, Skill.MosquitoStingerPoison, Skill.MosquitoStingerPoisonResist } },
        { Skill.MosquitoStingerSheepDeath, new [] { Skill.MosquitoStingerInfection } },
    
        { Skill.HermitFireResist, new [] { Skill.NoSkill } },
        { Skill.HermitPoisonResist, new [] { Skill.NoSkill } },
        { Skill.HermitDebuffRemove, new [] { Skill.NoSkill } },
        { Skill.HermitRange, new [] { Skill.HermitFireResist, Skill.HermitPoisonResist, Skill.HermitDebuffRemove } },
        { Skill.HermitAggro, new [] { Skill.HermitFireResist, Skill.HermitPoisonResist, Skill.HermitDebuffRemove } },
        { Skill.HermitReflection, new [] { Skill.HermitRange, Skill.HermitAggro } },
        { Skill.HermitFaint, new [] { Skill.HermitRange, Skill.HermitAggro } },
    };
}