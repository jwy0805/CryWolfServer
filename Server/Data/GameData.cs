using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Data;

public class GameData
{ // Game 초기 설정 - 불변 정보, 모든 GameRoom Instance에서 공유
    public static readonly float GroundHeight = 6.0f;
    public static readonly float AirHeight = 9.0f;
    public static Vector3 Center = new(0.0f, 6.0f, 0.0f); // Center of the Map
    public static readonly int[] ZCoordinatesOfMap = { 112, 84, 52, 20, 0, -20, -52, -84, -112 }; // Vector2Int, Vector3 * 4
    
    public static Vector3[] SpawnerPos { get; set; } = {
        new(0.0f, 6.0f, 30.0f), // North
        new(0.0f, 6.0f, -30.0f) // South
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

    public static readonly Dictionary<UnitId, HashSet<Skill>> OwnSkills = new()
    {
        { UnitId.Bunny, new HashSet<Skill> 
            { Skill.BunnyHealth, Skill.BunnyEvasion } },
        { UnitId.Rabbit, new HashSet<Skill> 
            { Skill.RabbitAggro, Skill.RabbitDefence, Skill.RabbitEvasion } },
        { UnitId.Hare, new HashSet<Skill> 
            { Skill.HareEvasion, Skill.HarePunch, Skill.HarePunchDefenceDown } },
        { UnitId.Mushroom, new HashSet<Skill> 
            { Skill.MushroomAttack, Skill.MushroomRange, Skill.MushroomClosestAttack } },
        { UnitId.Fungi, new HashSet<Skill> 
            { Skill.FungiPoison, Skill.FungiClosestHeal, Skill.FungiPoisonResist } },
        { UnitId.Toadstool, new HashSet<Skill> 
            { Skill.ToadstoolPoisonImmunity, Skill.ToadstoolPoisonResist, Skill.ToadstoolClosestAttackAll } },
        { UnitId.Seed, new HashSet<Skill> 
            { Skill.SeedEvasion, Skill.SeedRange } },
        { UnitId.Sprout, new HashSet<Skill> 
            { Skill.SproutDrain, Skill.SproutFireAttack, Skill.SproutFireResist } },
        { UnitId.FlowerPot, new HashSet<Skill> 
            { Skill.FlowerPot3Hit, Skill.FlowerPotRecoverBurn } },
        { UnitId.Bud, new HashSet<Skill> 
            { Skill.BudAttackSpeed, Skill.BudRange, Skill.BudAccuracy } },
        { UnitId.Bloom, new HashSet<Skill> 
            { Skill.Bloom3Combo, Skill.BloomCritical, Skill.BloomCriticalDamage } },
        { UnitId.Blossom, new HashSet<Skill> 
            { Skill.BlossomAttack, Skill.BlossomDeath } },
        { UnitId.PracticeDummy, new HashSet<Skill> 
            { Skill.PracticeDummyHealth, Skill.PracticeDummyHealth2 } },
        { UnitId.TargetDummy, new HashSet<Skill> 
            { Skill.TargetDummyHealSelf, Skill.TargetDummyPoisonResist, Skill.TargetDummyAggro } },
        { UnitId.TrainingDummy, new HashSet<Skill> 
            { Skill.TrainingDummyFaintAttack, Skill.TrainingDummyAccuracy, Skill.TrainingDummyHealth } },
        { UnitId.Shell, new HashSet<Skill> 
            { Skill.ShellDefence, Skill.ShellDefence2 } },
        { UnitId.Spike, new HashSet<Skill> 
            { Skill.SpikeReflection, Skill.SpikeFireResist, Skill.SpikePoisonResist } },
        { UnitId.Hermit, new HashSet<Skill> 
            { Skill.HermitNormalAttackDefence, Skill.HermitAttackerFaint, Skill.HermitFireResist } },
        { UnitId.SunBlossom, new HashSet<Skill> 
            { Skill.SunBlossomHeal, Skill.SunBlossomSelfDefence, Skill.SunBlossomDefence } },
        { UnitId.SunflowerFairy, new HashSet<Skill> 
            { Skill.SunflowerFairyFenceHeal, Skill.SunflowerFairyShield, Skill.SunflowerFairyHealParamUp,
                Skill.SunflowerFairyDoubleBuff } },
        { UnitId.SunfloraPixie, new HashSet<Skill> 
            { Skill.SunfloraPixieRecoverMp, Skill.SunfloraPixieStrongAttack, Skill.SunfloraPixieInvincible,
                Skill.SunfloraPixieDebuffRemove, Skill.SunfloraPixieTripleBuff } },
        { UnitId.MothLuna, new HashSet<Skill> 
            { Skill.MothLunaAccuracy, Skill.MothLunaRange } },
        { UnitId.MothMoon, new HashSet<Skill> 
            { Skill.MothMoonSheepHeal, Skill.MothMoonSheepShield, Skill.MothMoonSheepDebuffRemove } },
        { UnitId.MothCelestial, new HashSet<Skill> 
            { Skill.MothCelestialSheepDebuffRemove, Skill.MothCelestialSheepHealParamUp, Skill.MothCelestialBreed,
                Skill.MothCelestialPoison, Skill.MothCelestialAccuracy } },
        { UnitId.Soul, new HashSet<Skill> 
            { Skill.SoulAttack, Skill.SoulDrain, Skill.SoulAttackSpeed } },
        { UnitId.Haunt, new HashSet<Skill> 
            { Skill.HauntFire, Skill.HauntRange, Skill.HauntFireResist, Skill.HauntPoisonResist } },
        { UnitId.SoulMage, new HashSet<Skill> 
            { Skill.SoulMageCritical, Skill.SoulMageDebuffResist, Skill.SoulMageDragonPunch,
                Skill.SoulMageMagicPortal, Skill.SoulMageShareDamage } },

        { UnitId.DogPup, new HashSet<Skill> 
            { Skill.DogPupEvasion, Skill.DogPupSpeed, Skill.DogPupAttackSpeed } },
        { UnitId.DogBark, new HashSet<Skill> 
            { Skill.DogBarkAdjacentAttackSpeed, Skill.DogBarkFireResist, Skill.DogBarkFourthAttack } },
        { UnitId.DogBowwow, new HashSet<Skill> 
            { Skill.DogBowwowSmash, Skill.DogBowwowSmashFaint } },
        { UnitId.Burrow, new HashSet<Skill> 
            { Skill.BurrowHalfBurrow, Skill.BurrowDefence, Skill.BurrowEvasion, Skill.BurrowHealth } },
        { UnitId.MoleRat, new HashSet<Skill> 
            { Skill.MoleRatBurrowEvasion, Skill.MoleRatBurrowSpeed, Skill.MoleRatDrain, Skill.MoleRatStealAttack } },
        { UnitId.MoleRatKing, new HashSet<Skill> 
            { Skill.MoleRatKingBurrow, Skill.MoleRatKingStealWool } },
        { UnitId.MosquitoBug, new HashSet<Skill> 
            { Skill.MosquitoBugEvasion, Skill.MosquitoBugRange, Skill.MosquitoBugSpeed, Skill.MosquitoBugSheepFaint } },
        { UnitId.MosquitoPester, new HashSet<Skill> 
            { Skill.MosquitoPesterEvasion, Skill.MosquitoPesterHealth, Skill.MosquitoPesterPoison,
                Skill.MosquitoPesterPoisonResist, Skill.MosquitoPesterWoolRate } },
        { UnitId.MosquitoStinger, new HashSet<Skill> 
            { Skill.MosquitoStingerInfection, Skill.MosquitoStingerSheepDeath, Skill.MosquitoStingerWoolStop } },
        { UnitId.WolfPup, new HashSet<Skill> 
            { Skill.WolfPupAttack, Skill.WolfPupDefence, Skill.WolfPupSpeed } },
        { UnitId.Wolf, new HashSet<Skill> 
            { Skill.WolfCritical, Skill.WolfDrain, Skill.WolfHealth, Skill.WolfLastHitDna, Skill.WolfMagicalAttack } },
        { UnitId.Werewolf, new HashSet<Skill> 
            { Skill.WerewolfBerserker, Skill.WerewolfCriticalDamage,
                Skill.WerewolfCriticalRate, Skill.WerewolfThunder } },
        { UnitId.Bomb, new HashSet<Skill> 
            { Skill.BombAttack, Skill.BombBomb, Skill.BombHealth } },
        { UnitId.SnowBomb, new HashSet<Skill> 
            { Skill.SnowBombFrostbite, Skill.SnowBombAreaAttack, Skill.SnowBombFrostArmor, Skill.SnowBombFireResist } },
        { UnitId.PoisonBomb, new HashSet<Skill> 
            { Skill.PoisonBombBombRange, Skill.PoisonBombSelfDestruct, Skill.PoisonBombExplosionMpDown, Skill.PoisonBombPoisonPowerUp } },
        { UnitId.Cacti, new HashSet<Skill> 
            { Skill.CactiDefence, Skill.CactiDefence2, Skill.CactiHealth, Skill.CactiHealth2 } },
        { UnitId.Cactus, new HashSet<Skill> 
            { Skill.CactusPoisonResist, Skill.CactusReflection, Skill.CactusSpeed } },
        { UnitId.CactusBoss, new HashSet<Skill> 
            { Skill.CactusBossRush, Skill.CactusBossBreath, Skill.CactusBossHeal, Skill.CactusBossAggro } },
        { UnitId.Snakelet, new HashSet<Skill> 
            { Skill.SnakeletAttack, Skill.SnakeletEvasion, Skill.SnakeletAttackSpeed } },
        { UnitId.Snake, new HashSet<Skill> 
            { Skill.SnakeAccuracy, Skill.SnakeFire, Skill.SnakeFireResist, Skill.SnakeSpeed } },
        { UnitId.SnakeNaga, new HashSet<Skill> 
            { Skill.SnakeNagaCritical, Skill.SnakeNagaDrain, Skill.SnakeNagaMeteor, 
                Skill.SnakeNagaBigFire, Skill.SnakeNagaSuperAccuracy } },
        { UnitId.Lurker, new HashSet<Skill>
            { Skill.LurkerSpeed, Skill.LurkerDefence, Skill.LurkerPoisonResist } },
        { UnitId.Creeper, new HashSet<Skill>
            { Skill.CreeperPoison, Skill.CreeperRoll, Skill.CreeperNestedPoison, Skill.CreeperRollDamageUp } },
        { UnitId.Horror, new HashSet<Skill> 
            { Skill.HorrorPoisonSmog, Skill.HorrorPoisonImmunity, Skill.HorrorRollPoison, Skill.HorrorDegeneration, Skill.HorrorDivision } },
        { UnitId.Skeleton, new HashSet<Skill> 
            { Skill.SkeletonDefenceDown, Skill.SkeletonNestedDebuff, Skill.SkeletonAdditionalDamage, Skill.SkeletonAttackSpeed } },
        { UnitId.SkeletonGiant, new HashSet<Skill> 
            { Skill.SkeletonGiantAttackSteal, Skill.SkeletonGiantRevive,
                Skill.SkeletonGiantMpDown, Skill.SkeletonGiantDefenceDebuff } },
        { UnitId.SkeletonMage, new HashSet<Skill> 
            { Skill.SkeletonMageAdjacentRevive, Skill.SkeletonMageKillRecoverMp,
                Skill.SkeletonMageReviveHealthUp, Skill.SkeletonMageCurse } }
    };
    
    public static readonly Dictionary<Skill, HashSet<Skill>> SkillTree = new()
    {
        { Skill.BunnyHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BunnyEvasion, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.RabbitAggro, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.RabbitDefence, new HashSet<Skill> { Skill.RabbitAggro } },
        { Skill.RabbitEvasion, new HashSet<Skill> { Skill.RabbitDefence } },
        { Skill.HarePunch, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.HarePunchDefenceDown, new HashSet<Skill> { Skill.HarePunch } },
        { Skill.HareEvasion, new HashSet<Skill> { Skill.HarePunchDefenceDown } },
        { Skill.MushroomAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MushroomRange, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MushroomClosestAttack, new HashSet<Skill> { Skill.MushroomAttack, Skill.MushroomRange } },
        { Skill.FungiPoison, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.FungiPoisonResist, new HashSet<Skill> { Skill.FungiPoison } },
        { Skill.FungiClosestHeal, new HashSet<Skill> { Skill.FungiPoisonResist } },
        { Skill.ToadstoolClosestAttackAll, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.ToadstoolPoisonResist, new HashSet<Skill> { Skill.ToadstoolClosestAttackAll } },
        { Skill.ToadstoolPoisonImmunity, new HashSet<Skill> { Skill.ToadstoolPoisonResist } },
        { Skill.SeedEvasion, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SeedRange, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SproutDrain, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SproutFireAttack, new HashSet<Skill> { Skill.SproutDrain } },
        { Skill.SproutFireResist, new HashSet<Skill> { Skill.SproutFireAttack } },
        { Skill.FlowerPot3Hit, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.FlowerPotRecoverBurn, new HashSet<Skill> { Skill.FlowerPot3Hit } },
        { Skill.BudAttackSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BudRange, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BudAccuracy, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.Bloom3Combo, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BloomCritical, new HashSet<Skill> { Skill.Bloom3Combo } },
        { Skill.BloomCriticalDamage, new HashSet<Skill> { Skill.BloomCritical } },
        { Skill.BlossomAttack, new HashSet<Skill> { Skill.BlossomAttack } },
        { Skill.BlossomDeath, new HashSet<Skill> { Skill.BlossomAttack } },
        { Skill.PracticeDummyHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.PracticeDummyHealth2, new HashSet<Skill> { Skill.PracticeDummyHealth } },
        { Skill.TargetDummyHealSelf, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.TargetDummyPoisonResist, new HashSet<Skill> { Skill.TargetDummyHealSelf } },
        { Skill.TargetDummyAggro, new HashSet<Skill> { Skill.TargetDummyHealSelf } },
        { Skill.TrainingDummyFaintAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.TrainingDummyAccuracy, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.TrainingDummyHealth, new HashSet<Skill> { Skill.TrainingDummyFaintAttack, Skill.TrainingDummyAccuracy } },
        { Skill.ShellDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.ShellDefence2, new HashSet<Skill> { Skill.ShellDefence } },
        { Skill.SpikeReflection, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SpikeFireResist, new HashSet<Skill> { Skill.SpikeReflection } },
        { Skill.SpikePoisonResist, new HashSet<Skill> { Skill.SpikeReflection } },
        { Skill.HermitNormalAttackDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.HermitAttackerFaint, new HashSet<Skill> { Skill.HermitNormalAttackDefence } },
        { Skill.HermitFireResist, new HashSet<Skill> { Skill.HermitAttackerFaint } },
        { Skill.SunBlossomHeal, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunBlossomSelfDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunBlossomDefence, new HashSet<Skill> { Skill.SunBlossomHeal, Skill.SunBlossomSelfDefence } },
        { Skill.SunflowerFairyFenceHeal, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunflowerFairyShield, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunflowerFairyHealParamUp, new HashSet<Skill> { Skill.SunflowerFairyFenceHeal, Skill.SunflowerFairyShield } },
        { Skill.SunflowerFairyDoubleBuff, new HashSet<Skill> { Skill.SunflowerFairyHealParamUp } },
        { Skill.SunfloraPixieRecoverMp, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunfloraPixieStrongAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SunfloraPixieInvincible, new HashSet<Skill> { Skill.SunfloraPixieRecoverMp, Skill.SunfloraPixieStrongAttack } },
        { Skill.SunfloraPixieDebuffRemove, new HashSet<Skill> { Skill.SunfloraPixieRecoverMp, Skill.SunfloraPixieStrongAttack } },
        { Skill.SunfloraPixieTripleBuff, new HashSet<Skill> { Skill.SunfloraPixieInvincible } },
        { Skill.MothLunaAccuracy, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothLunaRange, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothMoonSheepHeal, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothMoonSheepShield, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothMoonSheepDebuffRemove, new HashSet<Skill> { Skill.SheepHealth, Skill.MothMoonSheepShield } },
        { Skill.MothMoonAttackSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothCelestialSheepHealParamUp, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothCelestialBreed, new HashSet<Skill> { Skill.MothCelestialSheepHealParamUp } },
        { Skill.MothCelestialPoison, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothCelestialAccuracy, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MothCelestialSheepDebuffRemove, new HashSet<Skill> { Skill.MothCelestialBreed, Skill.MothCelestialPoison, Skill.MothCelestialAccuracy } },
        { Skill.SoulAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SoulAttackSpeed, new HashSet<Skill> { Skill.SoulAttack } },
        { Skill.SoulDrain, new HashSet<Skill> { Skill.SoulAttackSpeed } },
        { Skill.HauntFire, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.HauntRange, new HashSet<Skill> { Skill.HauntFire } },
        { Skill.HauntFireResist, new HashSet<Skill> { Skill.HauntRange } },
        { Skill.HauntPoisonResist, new HashSet<Skill> { Skill.HauntRange } },
        { Skill.SoulMageDragonPunch, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SoulMageShareDamage, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SoulMageMagicPortal, new HashSet<Skill> { Skill.SoulMageDragonPunch } },
        { Skill.SoulMageDebuffResist, new HashSet<Skill> { Skill.SoulMageShareDamage } },
        { Skill.SoulMageCritical, new HashSet<Skill> { Skill.SoulMageMagicPortal, Skill.SoulMageDebuffResist } },

        { Skill.DogPupSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.DogPupEvasion, new HashSet<Skill> { Skill.DogPupSpeed } },
        { Skill.DogPupAttackSpeed, new HashSet<Skill> { Skill.DogPupEvasion } },
        { Skill.DogBarkAdjacentAttackSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.DogBarkFireResist, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.DogBarkFourthAttack, new HashSet<Skill> { Skill.DogBarkAdjacentAttackSpeed, Skill.DogBarkFireResist } },
        { Skill.DogBowwowSmash, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.DogBowwowSmashFaint, new HashSet<Skill> { Skill.DogBowwowSmash } },
        { Skill.BurrowHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BurrowDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BurrowEvasion, new HashSet<Skill> { Skill.BurrowHealth, Skill.BurrowDefence } },
        { Skill.BurrowHalfBurrow, new HashSet<Skill> { Skill.BurrowEvasion } },
        { Skill.MoleRatBurrowSpeed, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MoleRatBurrowEvasion, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MoleRatDrain, new HashSet<Skill>{ Skill.MoleRatBurrowSpeed, Skill.MoleRatBurrowEvasion } },
        { Skill.MoleRatStealAttack, new HashSet<Skill>{ Skill.MoleRatDrain } },
        { Skill.MoleRatKingBurrow, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MoleRatKingStealWool, new HashSet<Skill>{ Skill.MoleRatKingBurrow } },
        { Skill.MosquitoBugSpeed, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MosquitoBugEvasion, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MosquitoBugRange, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MosquitoBugSheepFaint, new HashSet<Skill>{ Skill.MosquitoBugSpeed, Skill.MosquitoBugEvasion, Skill.MosquitoBugRange } },
        { Skill.MosquitoPesterPoison, new HashSet<Skill>{ Skill.NoSkill } },
        { Skill.MosquitoPesterWoolRate, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MosquitoPesterPoisonResist, new HashSet<Skill> { Skill.MosquitoPesterPoison } },
        { Skill.MosquitoPesterEvasion, new HashSet<Skill> { Skill.MosquitoPesterWoolRate } },
        { Skill.MosquitoPesterHealth, new HashSet<Skill> { Skill.MosquitoPesterEvasion } },
        { Skill.MosquitoStingerWoolStop, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MosquitoStingerInfection, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.MosquitoStingerSheepDeath, new HashSet<Skill> { Skill.MosquitoStingerWoolStop, Skill.MosquitoStingerInfection } },
        { Skill.WolfPupSpeed, new HashSet<Skill> { Skill.WolfPupDefence, Skill.WolfPupAttack } },
        { Skill.WolfPupAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WolfPupDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WolfHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WolfMagicalAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WolfDrain, new HashSet<Skill> { Skill.WolfHealth } },
        { Skill.WolfCritical, new HashSet<Skill> { Skill.WolfMagicalAttack } },
        { Skill.WolfLastHitDna, new HashSet<Skill> { Skill.WolfDrain, Skill.WolfCritical } },
        { Skill.WerewolfThunder, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WerewolfCriticalRate, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.WerewolfCriticalDamage, new HashSet<Skill> { Skill.WerewolfCriticalRate } },
        { Skill.WerewolfBerserker, new HashSet<Skill> { Skill.WerewolfThunder, Skill.WerewolfCriticalDamage } },
        { Skill.BombHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.BombAttack, new HashSet<Skill> { Skill.BombHealth } },
        { Skill.BombBomb, new HashSet<Skill> { Skill.BombAttack } },
        { Skill.SnowBombFireResist, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnowBombAreaAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnowBombFrostbite, new HashSet<Skill> { Skill.SnowBombFireResist, Skill.SnowBombAreaAttack } },
        { Skill.SnowBombFrostArmor, new HashSet<Skill> { Skill.SnowBombFrostbite } },
        { Skill.PoisonBombBombRange, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.PoisonBombSelfDestruct, new HashSet<Skill> { Skill.PoisonBombBombRange } },
        { Skill.PoisonBombPoisonPowerUp, new HashSet<Skill> { Skill.PoisonBombSelfDestruct } },
        { Skill.PoisonBombExplosionMpDown, new HashSet<Skill> { Skill.PoisonBombPoisonPowerUp } },
        { Skill.CactiHealth, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CactiHealth2, new HashSet<Skill> { Skill.CactiHealth } },
        { Skill.CactiDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CactiDefence2, new HashSet<Skill> { Skill.CactiDefence } },
        { Skill.CactusSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CactusPoisonResist, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CactusReflection, new HashSet<Skill> { Skill.CactusSpeed, Skill.CactusPoisonResist } },
        { Skill.CactusReflectionFaint, new HashSet<Skill> { Skill.CactusReflection } },
        { Skill.CactusBossRush, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CactusBossBreath, new HashSet<Skill> { Skill.CactusBossRush } },
        { Skill.CactusBossAggro, new HashSet<Skill> { Skill.CactusBossBreath } },
        { Skill.CactusBossHeal, new HashSet<Skill> { Skill.CactusBossBreath } },
        { Skill.SnakeletAttackSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnakeletAttack, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnakeletEvasion, new HashSet<Skill> { Skill.SnakeletAttackSpeed, Skill.SnakeletAttack } },
        { Skill.SnakeFire, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnakeAccuracy, new HashSet<Skill> { Skill.SnakeFire } },
        { Skill.SnakeFireResist, new HashSet<Skill> { Skill.SnakeFire } },
        { Skill.SnakeSpeed, new HashSet<Skill> { Skill.SnakeFire } },
        { Skill.SnakeNagaBigFire, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnakeNagaDrain, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SnakeNagaCritical, new HashSet<Skill> { Skill.SnakeNagaBigFire } },
        { Skill.SnakeNagaSuperAccuracy, new HashSet<Skill> { Skill.SnakeNagaDrain } },
        { Skill.SnakeNagaMeteor, new HashSet<Skill> { Skill.SnakeNagaCritical, Skill.SnakeNagaSuperAccuracy } },
        { Skill.LurkerSpeed, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.LurkerDefence, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.LurkerPoisonResist, new HashSet<Skill> { Skill.LurkerSpeed, Skill.LurkerDefence } },
        { Skill.CreeperPoison, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.CreeperRoll, new HashSet<Skill> { Skill.CreeperPoison } },
        { Skill.CreeperNestedPoison, new HashSet<Skill> { Skill.CreeperRoll } },
        { Skill.CreeperRollDamageUp, new HashSet<Skill> { Skill.CreeperRoll } },
        { Skill.HorrorPoisonImmunity, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.HorrorRollPoison, new HashSet<Skill> { Skill.HorrorPoisonImmunity } },
        { Skill.HorrorPoisonSmog, new HashSet<Skill> { Skill.HorrorPoisonImmunity, Skill.HorrorRollPoison } },
        { Skill.HorrorDegeneration, new HashSet<Skill> { Skill.HorrorPoisonImmunity } },
        { Skill.HorrorDivision, new HashSet<Skill> { Skill.HorrorPoisonImmunity, Skill.HorrorDegeneration } },
        { Skill.SkeletonDefenceDown, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SkeletonNestedDebuff, new HashSet<Skill> { Skill.SkeletonDefenceDown } },
        { Skill.SkeletonAdditionalDamage, new HashSet<Skill> { Skill.SkeletonDefenceDown, Skill.SkeletonNestedDebuff }},
        { Skill.SkeletonAttackSpeed, new HashSet<Skill> { Skill.SkeletonDefenceDown, Skill.SkeletonNestedDebuff }},
        { Skill.SkeletonGiantDefenceDebuff, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SkeletonGiantAttackSteal, new HashSet<Skill> { Skill.SkeletonGiantDefenceDebuff } },
        { Skill.SkeletonGiantMpDown, new HashSet<Skill> { Skill.SkeletonGiantAttackSteal } },
        { Skill.SkeletonGiantRevive, new HashSet<Skill> { Skill.SkeletonGiantAttackSteal } },
        { Skill.SkeletonMageAdjacentRevive, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SkeletonMageKillRecoverMp, new HashSet<Skill> { Skill.NoSkill } },
        { Skill.SkeletonMageReviveHealthUp, new HashSet<Skill> { Skill.SkeletonMageAdjacentRevive } },
        { Skill.SkeletonMageCurse, new HashSet<Skill> { Skill.SkeletonMageKillRecoverMp, Skill.SkeletonMageReviveHealthUp } }
    };
}