using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Data;

public class GameData
{
    // Game 초기 설정
    
    // 불변
    public static readonly float GroundHeight = 6.0f;
    public static readonly float AirHeight = 9.0f;
    public static Vector3 Center = new(0.0f, 6.0f, 0.0f); // Center of the Map
    public static readonly int[] ZCoordinatesOfMap = { 112, 84, 52, 20, 0, -20, -52, -84, -112 }; // Vector2Int, Vector3 * 4
    
    public static Vector3[] SpawnerPos { get; set; } = {
        new(0.0f, 6.0f, 25.0f), // North
        new(0.0f, 6.0f, -25.0f) // South
    };
    
    #region FenceData

    public static readonly string[] FenceName = { "", "FenceLv1", "FenceLv2", "FenceLv3" };
    public static readonly int NorthFenceMax = 8;
    public static readonly int SouthFenceMax = 8;
    public static readonly Vector3 FenceStartPos = new(-7, 6, -5);
    public static readonly Vector3 FenceCenter = new(0, 6, 0);
    public static readonly Vector3 FenceSize = new(12, 6, 10);
    
    public static readonly List<Vector3> FenceBounds = new()
    {
        new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2),
        new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
        new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
        new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2)
    };

    public static readonly List<Vector3> SheepBounds = new()
    {
        new Vector3(Center.X - 4f, Center.Y, Center.Z + 3f),
        new Vector3(Center.X - 4f, Center.Y, Center.Z - 3f),
        new Vector3(Center.X + 6f, Center.Y, Center.Z - 3f),
        new Vector3(Center.X + 6f, Center.Y, Center.Z + 3f)
    };
        
    public static Vector3[] GetPos(int cnt, int row, Vector3 startPos)
    {
        Vector3[] posArr = new Vector3[cnt];

        for (int i = 0; i < row; i++)
        {
            posArr[i] = startPos with { X = startPos.X + i * 2, Z = -5}; // south fence
            posArr[row + i] = startPos with { X = startPos.X + i * 2, Z = 5 }; // north fence
        }

        return posArr;
    }
    
    public static float[] GetRotation(int cnt, int row)
    {
        float[] rotationArr = new float[cnt];
        
        for (int i = 0; i < row; i++)
        {
            rotationArr[i] = 180;
            rotationArr[row + i] = 0;
        }

        return rotationArr;
    }

    #endregion

    // 게임 진행 정보
    #region GameInfo

    public static readonly long RoundTime = 20000;
    public static int[] StorageLvUpCost = { 0, 600, 2000 };
    
    #endregion

    public static readonly Dictionary<TowerId, Skill[]> OwnSkills = new()
    {
        {
            TowerId.Bud, new[] { Skill.BudAttack, Skill.BudDouble, Skill.BudRange, Skill.BudSeed, Skill.BudAttackSpeed }
        },
        {
            TowerId.Bloom,
            new[]
            {
                Skill.Bloom3Combo, Skill.BloomAttack, Skill.BloomRange, Skill.BloomAirAttack, Skill.BloomAttackSpeed,
                Skill.BloomAttackSpeed2
            }
        },
        {
            TowerId.Blossom,
            new[]
            {
                Skill.BlossomAccuracy, Skill.BlossomAttack, Skill.BlossomDeath, Skill.BlossomPoison, Skill.BlossomRange,
                Skill.BlossomAttackSpeed
            }
        },
        {
            TowerId.PracticeDummy,
            new[]
            {
                Skill.PracticeDummyAggro, Skill.PracticeDummyDefence, Skill.PracticeDummyDefence2,
                Skill.PracticeDummyHealth, Skill.PracticeDummyHealth2
            }
        },
        {
            TowerId.TargetDummy,
            new[]
            {
                Skill.TargetDummyHeal, Skill.TargetDummyHealth, Skill.TargetDummyReflection,
                Skill.TargetDummyFireResist, Skill.TargetDummyPoisonResist
            }
        },
        {
            TowerId.TrainingDummy,
            new[]
            {
                Skill.TrainingDummyAggro, Skill.TrainingDummyDefence, Skill.TrainingDummyFaint, Skill.TrainingDummyHeal,
                Skill.TrainingDummyHealth, Skill.TrainingDummyDebuffRemove, Skill.TrainingDummyFireResist,
                Skill.TrainingDummyPoisonResist
            }
        },
        {
            TowerId.SunBlossom,
            new[] { Skill.SunBlossomHeal, Skill.SunBlossomHealth, Skill.SunBlossomSlow, Skill.SunBlossomSlowAttack }
        },
        {
            TowerId.SunflowerFairy,
            new[]
            {
                Skill.SunflowerFairyAttack, Skill.SunflowerFairyDefence, Skill.SunflowerFairyDouble,
                Skill.SunflowerFairyFenceHeal, Skill.SunflowerFairyMpDown
            }
        },
        {
            TowerId.SunfloraPixie,
            new[]
            {
                Skill.SunfloraPixieAttack, Skill.SunfloraPixieCurse, Skill.SunfloraPixieFaint, Skill.SunfloraPixieHeal,
                Skill.SunfloraPixieInvincible, Skill.SunfloraPixieRange, Skill.SunfloraPixieTriple,
                Skill.SunfloraPixieAttackSpeed, Skill.SunfloraPixieDebuffRemove
            }
        },
        {
            TowerId.MothLuna,
            new[] { Skill.MothLunaAccuracy, Skill.MothLunaAttack, Skill.MothLunaFaint, Skill.MothLunaSpeed }
        },
        {
            TowerId.MothMoon,
            new[]
            {
                Skill.MothMoonOutput, Skill.MothMoonRange, Skill.MothMoonAttackSpeed, Skill.MothMoonHealSheep,
                Skill.MothMoonRemoveDebuffSheep
            }
        },
        {
            TowerId.MothCelestial,
            new[]
            {
                Skill.MothCelestialAccuracy, Skill.MothCelestialPoison, Skill.MothCelestialBreedSheep,
                Skill.MothCelestialFireResist, Skill.MothCelestialGroundAttack, Skill.MothCelestialPoisonResist,
                Skill.MothCelestialSheepHealth
            }
        },
        { TowerId.Soul, new[] { Skill.SoulAttack, Skill.SoulDefence, Skill.SoulDrain, Skill.SoulHealth } },
        {
            TowerId.Haunt,
            new[]
            {
                Skill.HauntAttack, Skill.HauntFire, Skill.HauntAttackSpeed, Skill.HauntFireResist,
                Skill.HauntLongAttack, Skill.HauntPoisonResist
            }
        },
        {
            TowerId.SoulMage,
            new[]
            {
                Skill.SoulMageAvoid, Skill.SoulMageCritical, Skill.SoulMageTornado, Skill.SoulMageDebuffResist,
                Skill.SoulMageDefenceAll, Skill.SoulMageFireDamage, Skill.SoulMageNatureAttack,
                Skill.SoulMageShareDamage
            }
        },
    };
    
    public static readonly Dictionary<Skill, Skill[]> SkillTree = new()
    {
        { Skill.BloomAttack, new[] { Skill.NoSkill } },
        { Skill.BloomAttackSpeed, new[] { Skill.NoSkill } },
        { Skill.BloomRange, new[] { Skill.BloomAirAttack } },
        { Skill.BloomAttackSpeed2, new[] { Skill.BloomAttackSpeed } },
        { Skill.BloomAirAttack, new[] { Skill.NoSkill } },
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
        { Skill.MothCelestialFireResist, new[] { Skill.MothCelestialSheepHealth, Skill.MothCelestialAccuracy } },
        { Skill.MothCelestialPoisonResist, new[] { Skill.MothCelestialSheepHealth, Skill.MothCelestialAccuracy } },
        { Skill.MothCelestialPoison, new[] { Skill.MothCelestialGroundAttack } },
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