using Google.Protobuf.Protocol;
using Server.Game.etc;
using Server.Game.Object.etc;
using Server.Game.Resources;

namespace Server.Game;

public sealed partial class ObjectManager
{
    private readonly Dictionary<UnitId, IFactory<Tower>> _towerDict = new()
    {
        { UnitId.Bunny, new BunnyFactory() },
        { UnitId.Rabbit, new RabbitFactory() },
        { UnitId.Hare, new HareFactory() },
        { UnitId.Mushroom, new MushroomFactory() },
        { UnitId.Fungi, new FungiFactory() },
        { UnitId.Toadstool, new ToadstoolFactory() },
        { UnitId.Seed, new SeedFactory() },
        { UnitId.Sprout, new SproutFactory() },
        { UnitId.FlowerPot, new FlowerPotFactory() },
        { UnitId.Bud, new BudFactory() },
        { UnitId.Bloom, new BloomFactory() },
        { UnitId.Blossom, new BlossomFactory() },
        { UnitId.PracticeDummy, new PracticeDummyFactory() },
        { UnitId.TargetDummy, new TargetDummyFactory() },
        { UnitId.TrainingDummy, new TrainingDummyFactory() },
        { UnitId.Shell, new ShellFactory() },
        { UnitId.Spike, new SpikeFactory() },
        { UnitId.Hermit, new HermitFactory() },
        { UnitId.SunBlossom, new SunBlossomFactory() },
        { UnitId.SunflowerFairy, new SunflowerFairyFactory() },
        { UnitId.SunfloraPixie, new SunfloraPixieFactory() },
        { UnitId.MothLuna, new MothLunaFactory() },
        { UnitId.MothMoon, new MothMoonFactory() },
        { UnitId.MothCelestial, new MothCelestialFactory() },
        { UnitId.Soul, new SoulFactory() },
        { UnitId.Haunt, new HauntFactory() },
        { UnitId.SoulMage, new SoulMageFactory() },
    };
    
    private readonly Dictionary<UnitId, IFactory<Monster>> _monsterDict = new()
    {
        { UnitId.DogPup, new DogPupFactory() },
        { UnitId.DogBark, new DogBarkFactory() },
        { UnitId.DogBowwow, new DogBowwowFactory() },
        { UnitId.Burrow, new BurrowFactory() },
        { UnitId.MoleRat, new MoleRatFactory() },
        { UnitId.MoleRatKing, new MoleRatKingFactory() },
        { UnitId.WolfPup, new WolfPupFactory() },
        { UnitId.Wolf, new WolfFactory() },
        { UnitId.Werewolf, new WerewolfFactory() },
        { UnitId.Bomb, new BombFactory() },
        { UnitId.SnowBomb, new SnowBombFactory() },
        { UnitId.PoisonBomb, new PoisonBombFactory() },
        { UnitId.Cacti, new CactiFactory() },
        { UnitId.Cactus, new CactusFactory() },
        { UnitId.CactusBoss, new CactusBossFactory() },
        { UnitId.Lurker, new LurkerFactory() },
        { UnitId.Creeper, new CreeperFactory() },
        { UnitId.Horror, new HorrorFactory() },
        { UnitId.Snakelet, new SnakeletFactory() },
        { UnitId.Snake, new SnakeFactory() },
        { UnitId.SnakeNaga, new SnakeNagaFactory() },
        { UnitId.MosquitoBug, new MosquitoBugFactory() },
        { UnitId.MosquitoPester, new MosquitoPesterFactory() },
        { UnitId.MosquitoStinger, new MosquitoStingerFactory() },
        { UnitId.Skeleton, new SkeletonFactory() },
        { UnitId.SkeletonGiant, new SkeletonGiantFactory() },
        { UnitId.SkeletonMage, new SkeletonMageFactory() }
    };
    
    private readonly Dictionary<ProjectileId, IFactory<Projectile>> _projectileDict = new()
    {
        { ProjectileId.BasicProjectile, new BasicProjectileFactory() },
        { ProjectileId.SnakeFire, new SnakeFireFactory() },
        { ProjectileId.SnakeNagaFire, new SnakeNagaFireFactory() },
        { ProjectileId.SnakeNagaBigFire, new SnakeNagaBigFireFactory() },
        { ProjectileId.SmallPoison, new SmallPoisonFactory() },
        { ProjectileId.BigPoison, new BigPoisonFactory() },
        { ProjectileId.SeedProjectile, new SeedProjectileFactory() },
        { ProjectileId.BlossomProjectile, new BlossomProjectileFactory() },
        { ProjectileId.BlossomDeathProjectile, new BlossomDeathProjectileFactory() },
        { ProjectileId.HauntProjectile, new HauntProjectileFactory() },
        { ProjectileId.HauntFire, new HauntFireFactory() },
        { ProjectileId.SoulMageProjectile, new SoulMageProjectileFactory() },
        { ProjectileId.SunfloraPixieProjectile, new SunfloraPixieProjectileFactory() },
        { ProjectileId.SunfloraPixieFire, new SunfloraPixieFireFactory() },
        { ProjectileId.MothMoonProjectile, new MothMoonProjectileFactory()},
        { ProjectileId.MothCelestialPoison, new MothCelestialPoisonFactory() },
        { ProjectileId.MosquitoStingerProjectile, new MosquitoStingerProjectileFactory() },
        { ProjectileId.SpikeProjectile, new SpikeProjectileFactory() },
        { ProjectileId.HermitProjectile, new HermitProjectileFactory() },
        { ProjectileId.MosquitoPesterProjectile, new MosquitoPesterProjectileFactory() },
        { ProjectileId.BombProjectile, new BombProjectileFactory()},
        { ProjectileId.BombSkill, new BombSkillFactory()},
        { ProjectileId.SnowBombSkill, new SnowBombSkillFactory() },
        { ProjectileId.PoisonBombSkill, new PoisonBombSkillFactory() },
        { ProjectileId.SkeletonMageProjectile, new SkeletonMageProjectileFactory() },
        { ProjectileId.BasicProjectile2, new BasicProjectile2Factory() },
        { ProjectileId.HarePunch, new HarePunchFactory() },
        { ProjectileId.RabbitAggro, new RabbitAggroFactory() },
        { ProjectileId.BasicProjectile3, new BasicProjectile3Factory() },
        { ProjectileId.BasicProjectile4, new BasicProjectile4Factory() }
    };
    
    private readonly Dictionary<EffectId, IFactory<Effect>> _effectDict = new()
    {
        { EffectId.LightningStrike, new LightningStrikeFactory() },
        { EffectId.PoisonSmog, new PoisonBeltFactory() },
        { EffectId.HolyAura, new HolyAuraFactory() },
        { EffectId.SoulMagePunch, new SoulMagePunchFactory() },
        { EffectId.Meteor, new MeteorFactory() },
        { EffectId.StateSlow, new StateSlowFactory() },
        { EffectId.StatePoison, new StatePoisonFactory() },
        { EffectId.StateFaint, new StateFaintFactory() },
        { EffectId.StateCurse, new StateCurseFactory() },
        { EffectId.StateBurn, new StateBurnFactory() },
        { EffectId.StateHeal, new StateHealFactory() },
        { EffectId.StateDebuffRemove, new StateDebuffRemoveFactory() },
        { EffectId.StateAggro, new StateAggroFactory() },
        { EffectId.GreenGate, new GreenGateFactory() },
        { EffectId.NaturalTornado, new NaturalTornadoFactory() },
        { EffectId.PurpleBeam, new PurpleBeamFactory() },
        { EffectId.StarFall, new StarFallFactory() },
        { EffectId.HorrorRoll, new HorrorRollFactory()},
        { EffectId.Upgrade, new UpgradeEffectFactory() },
        { EffectId.SnowBombExplosion, new SnowBombExplosionFactory() },
        { EffectId.PoisonBombExplosion, new PoisonBombExplosionFactory() },
        { EffectId.BombSkillExplosion, new BombSkillExplosionFactory() },
        { EffectId.PoisonBombSkillExplosion, new PoisonBombSkillExplosionFactory() },
        { EffectId.SkeletonGiantEffect, new SkeletonGiantEffectFactory() },
        { EffectId.SkeletonGiantSkill, new SkeletonGiantSkillFactory() },
        { EffectId.SkeletonGiantRevive, new SkeletonGiantReviveFactory() },
        { EffectId.CactusBossBreathEffect, new CactusBossBreathEffectFactory() },
        { EffectId.CactusBossSmashEffect, new CactusBossSmashEffectFactory() },
        { EffectId.WolfMagicalEffect, new WolfMagicalEffectFactory() },
        { EffectId.WerewolfMagicalEffect, new WerewolfMagicalEffectFactory() },
        { EffectId.SkeletonEffect, new SkeletonEffectFactory() },
        { EffectId.SkeletonAdditionalEffect, new SkeletonAdditionalEffectFactory() },
        { EffectId.WillRevive, new WillReviveFactory() }
    };
    
    private readonly Dictionary<ResourceId, IFactory<Resource>> _resourceDict = new()
    {
        { ResourceId.CoinStarSilver, new CoinStarSilverFactory() },
        { ResourceId.CoinStarGolden, new CoinStarGoldenFactory() },
        { ResourceId.PouchGreen, new PouchGreenFactory() },
        { ResourceId.PouchRed, new PouchRedFactory() },
        { ResourceId.ChestGold, new ChestGoldFactory() }
    };

    public interface IFactory<out T> where T : GameObject
    {
        T Create();
    }
    
    public class MonsterStatueFactory { public MonsterStatue CreateStatue() => new(); }
    public class FenceFactory { public Fence CreateFence() => new(); }
    public class SheepFactory { }
    
    public class BunnyFactory : IFactory<Bunny> { public Bunny Create() => new(); }
    public class RabbitFactory : IFactory<Rabbit> { public Rabbit Create() => new(); }
    public class HareFactory : IFactory<Hare> { public Hare Create() => new(); }
    public class MushroomFactory : IFactory<Mushroom> { public Mushroom Create() => new(); }
    public class FungiFactory : IFactory<Fungi> { public Fungi Create() => new(); }
    public class ToadstoolFactory : IFactory<Toadstool> { public Toadstool Create() => new(); }
    public class SeedFactory : IFactory<Seed> { public Seed Create() => new(); }
    public class SproutFactory : IFactory<Sprout> { public Sprout Create() => new(); }
    public class FlowerPotFactory : IFactory<FlowerPot> { public FlowerPot Create() => new(); }
    public class BudFactory : IFactory<Bud> { public Bud Create() => new(); }
    public class BloomFactory : IFactory<Bloom> { public Bloom Create() => new(); }
    public class BlossomFactory : IFactory<Blossom> { public Blossom Create() => new(); }
    public class PracticeDummyFactory : IFactory<PracticeDummy> { public PracticeDummy Create() => new(); }
    public class TargetDummyFactory : IFactory<TargetDummy> { public TargetDummy Create() => new(); }
    public class TrainingDummyFactory : IFactory<TrainingDummy> { public TrainingDummy Create() => new(); }
    public class ShellFactory : IFactory<Shell> { public Shell Create() => new(); }
    public class SpikeFactory : IFactory<Spike> { public Spike Create() => new(); }
    public class HermitFactory : IFactory<Hermit> { public Hermit Create() => new(); }
    public class SunBlossomFactory : IFactory<SunBlossom> { public SunBlossom Create() => new(); }
    public class SunflowerFairyFactory : IFactory<SunflowerFairy> { public SunflowerFairy Create() => new(); }
    public class SunfloraPixieFactory : IFactory<SunfloraPixie> { public SunfloraPixie Create() => new(); }
    public class MothLunaFactory : IFactory<MothLuna> { public MothLuna Create() => new(); }
    public class MothMoonFactory : IFactory<MothMoon> { public MothMoon Create() => new(); }
    public class MothCelestialFactory : IFactory<MothCelestial> { public MothCelestial Create() => new(); }
    public class SoulFactory : IFactory<Soul> { public Soul Create() => new(); }
    public class HauntFactory : IFactory<Haunt> { public Haunt Create() => new(); }
    public class SoulMageFactory : IFactory<SoulMage> { public SoulMage Create() => new(); }
    public class DogPupFactory : IFactory<DogPup> { public DogPup Create() => new(); }
    public class DogBarkFactory : IFactory<DogBark> { public DogBark Create() => new(); }
    public class DogBowwowFactory : IFactory<DogBowwow> { public DogBowwow Create() => new(); }
    public class BurrowFactory : IFactory<Burrow> { public Burrow Create() => new(); }
    public class MoleRatFactory : IFactory<MoleRat> { public MoleRat Create() => new(); }
    public class MoleRatKingFactory : IFactory<MoleRatKing> { public MoleRatKing Create() => new(); }
    public class WolfPupFactory : IFactory<WolfPup> { public WolfPup Create() => new(); }
    public class WolfFactory : IFactory<Wolf> { public Wolf Create() => new(); }
    public class WerewolfFactory : IFactory<Werewolf> { public Werewolf Create() => new(); }
    public class BombFactory : IFactory<Bomb> { public Bomb Create() => new(); }
    public class SnowBombFactory : IFactory<SnowBomb> { public SnowBomb Create() => new(); }
    public class PoisonBombFactory : IFactory<PoisonBomb> { public PoisonBomb Create() => new(); }
    public class CactiFactory : IFactory<Cacti> { public Cacti Create() => new(); }
    public class CactusFactory : IFactory<Cactus> { public Cactus Create() => new(); }
    public class CactusBossFactory : IFactory<CactusBoss> { public CactusBoss Create() => new(); }
    public class LurkerFactory : IFactory<Lurker> { public Lurker Create() => new(); }
    public class CreeperFactory : IFactory<Creeper> { public Creeper Create() => new(); }
    public class HorrorFactory : IFactory<Horror> { public Horror Create() => new(); }
    public class SnakeletFactory : IFactory<Snakelet> { public Snakelet Create() => new(); }
    public class SnakeFactory : IFactory<Snake> { public Snake Create() => new(); }
    public class SnakeNagaFactory : IFactory<SnakeNaga> { public SnakeNaga Create() => new(); }
    public class MosquitoBugFactory : IFactory<MosquitoBug> { public MosquitoBug Create() => new(); }
    public class MosquitoPesterFactory : IFactory<MosquitoPester> { public MosquitoPester Create() => new(); }
    public class MosquitoStingerFactory : IFactory<MosquitoStinger> { public MosquitoStinger Create() => new(); }
    public class SkeletonFactory : IFactory<Skeleton> { public Skeleton Create() => new(); }
    public class SkeletonGiantFactory : IFactory<SkeletonGiant> { public SkeletonGiant Create() => new(); }
    public class SkeletonMageFactory : IFactory<SkeletonMage> { public SkeletonMage Create() => new(); }
    public class BasicProjectileFactory : IFactory<BasicProjectile> { public BasicProjectile Create() => new(); }
    public class SnakeFireFactory : IFactory<SnakeFire> { public SnakeFire Create() => new(); }
    public class SnakeNagaFireFactory : IFactory<SnakeNagaFire> { public SnakeNagaFire Create() => new(); }
    public class SnakeNagaBigFireFactory : IFactory<SnakeNagaBigFire> { public SnakeNagaBigFire Create() => new(); }
    public class SmallPoisonFactory : IFactory<SmallPoison> { public SmallPoison Create() => new(); }
    public class BigPoisonFactory : IFactory<BigPoison> { public BigPoison Create() => new(); }
    public class SeedProjectileFactory : IFactory<SeedProjectile> { public SeedProjectile Create() => new(); }
    public class BlossomProjectileFactory : IFactory<BlossomProjectile> { public BlossomProjectile Create() => new(); }
    public class BlossomDeathProjectileFactory : IFactory<BlossomDeathProjectile> { public BlossomDeathProjectile Create() => new(); }
    public class HauntProjectileFactory : IFactory<HauntProjectile> { public HauntProjectile Create() => new(); }
    public class HauntFireFactory : IFactory<HauntFire> { public HauntFire Create() => new(); }
    public class SoulMageProjectileFactory : IFactory<SoulMageProjectile> { public SoulMageProjectile Create() => new(); }
    public class SunfloraPixieProjectileFactory : IFactory<SunfloraPixieProjectile> { public SunfloraPixieProjectile Create() => new(); }
    public class SunfloraPixieFireFactory : IFactory<SunfloraPixieFire> { public SunfloraPixieFire Create() => new(); }
    public class MothMoonProjectileFactory : IFactory<MothMoonProjectile> { public MothMoonProjectile Create() => new(); }
    public class MothCelestialPoisonFactory : IFactory<MothCelestialPoison> { public MothCelestialPoison Create() => new(); }
    public class MosquitoStingerProjectileFactory : IFactory<MosquitoStingerProjectile> { public MosquitoStingerProjectile Create() => new(); }
    public class SpikeProjectileFactory : IFactory<SpikeProjectile> { public SpikeProjectile Create() => new(); }
    public class HermitProjectileFactory : IFactory<HermitProjectile> { public HermitProjectile Create() => new(); }
    public class MosquitoPesterProjectileFactory : IFactory<MosquitoPesterProjectile> { public MosquitoPesterProjectile Create() => new(); }
    public class BombProjectileFactory : IFactory<BombProjectile> { public BombProjectile Create() => new(); }
    public class BombSkillFactory : IFactory<BombSkill> { public BombSkill Create() => new(); }
    public class SnowBombSkillFactory : IFactory<SnowBombSkill> { public SnowBombSkill Create() => new(); }
    public class PoisonBombSkillFactory : IFactory<PoisonBombSkill> { public PoisonBombSkill Create() => new(); }
    public class SkeletonMageProjectileFactory : IFactory<SkeletonMageProjectile> { public SkeletonMageProjectile Create() => new(); }
    public class BasicProjectile2Factory : IFactory<BasicProjectile2> { public BasicProjectile2 Create() => new(); }
    public class HarePunchFactory : IFactory<HarePunch> { public HarePunch Create() => new(); }
    public class RabbitAggroFactory : IFactory<RabbitAggro> { public RabbitAggro Create() => new(); }
    public class BasicProjectile3Factory : IFactory<BasicProjectile3> { public BasicProjectile3 Create() => new(); }
    public class BasicProjectile4Factory : IFactory<BasicProjectile4> { public BasicProjectile4 Create() => new(); }
    public class LightningStrikeFactory : IFactory<LightningStrike> { public LightningStrike Create() => new(); }
    public class PoisonBeltFactory : IFactory<PoisonBelt> { public PoisonBelt Create() => new(); }
    public class HolyAuraFactory : IFactory<HolyAura> { public HolyAura Create() => new(); }
    public class SoulMagePunchFactory : IFactory<SoulMagePunch> { public SoulMagePunch Create() => new(); }
    public class MeteorFactory : IFactory<Meteor> { public Meteor Create() => new(); }
    public class StateSlowFactory : IFactory<StateSlow> { public StateSlow Create() => new(); }
    public class StatePoisonFactory : IFactory<StatePoison> { public StatePoison Create() => new(); }
    public class StateFaintFactory : IFactory<StateFaint> { public StateFaint Create() => new(); }
    public class StateCurseFactory : IFactory<StateCurse> { public StateCurse Create() => new(); }
    public class StateBurnFactory : IFactory<StateBurn> { public StateBurn Create() => new(); }
    public class StateHealFactory : IFactory<StateHeal> { public StateHeal Create() => new(); }
    public class StateDebuffRemoveFactory : IFactory<StateDebuffRemove> { public StateDebuffRemove Create() => new(); }
    public class StateAggroFactory : IFactory<StateAggro> { public StateAggro Create() => new(); }
    public class GreenGateFactory : IFactory<GreenGate> { public GreenGate Create() => new(); }
    public class NaturalTornadoFactory : IFactory<NaturalTornado> { public NaturalTornado Create() => new(); }
    public class PurpleBeamFactory : IFactory<PurpleBeam> { public PurpleBeam Create() => new(); }
    public class StarFallFactory : IFactory<StarFall> { public StarFall Create() => new(); }
    public class HorrorRollFactory : IFactory<HorrorRoll> { public HorrorRoll Create() => new(); }
    public class UpgradeEffectFactory : IFactory<UpgradeEffect> { public UpgradeEffect Create() => new(); }
    public class SnowBombExplosionFactory : IFactory<SnowBombExplosion> { public SnowBombExplosion Create() => new(); }
    public class PoisonBombExplosionFactory : IFactory<PoisonBombExplosion> { public PoisonBombExplosion Create() => new(); }
    public class BombSkillExplosionFactory : IFactory<BombSkillExplosion> { public BombSkillExplosion Create() => new(); }
    public class PoisonBombSkillExplosionFactory : IFactory<PoisonBombSkillExplosion> { public PoisonBombSkillExplosion Create() => new(); }
    public class SkeletonGiantEffectFactory : IFactory<SkeletonGiantEffect> { public SkeletonGiantEffect Create() => new(); }
    public class SkeletonGiantSkillFactory : IFactory<SkeletonGiantSkill> { public SkeletonGiantSkill Create() => new(); }
    public class SkeletonGiantReviveFactory : IFactory<SkeletonGiantRevive> { public SkeletonGiantRevive Create() => new(); }
    public class CactusBossBreathEffectFactory : IFactory<CactusBossBreathEffect> { public CactusBossBreathEffect Create() => new(); }
    public class CactusBossSmashEffectFactory : IFactory<CactusBossSmashEffect> { public CactusBossSmashEffect Create() => new(); }
    public class WolfMagicalEffectFactory : IFactory<WolfMagicalEffect> { public WolfMagicalEffect Create() => new(); }
    public class WerewolfMagicalEffectFactory : IFactory<WerewolfMagicalEffect> { public WerewolfMagicalEffect Create() => new(); }
    public class SkeletonEffectFactory : IFactory<SkeletonEffect> { public SkeletonEffect Create() => new(); }
    public class SkeletonAdditionalEffectFactory : IFactory<SkeletonAdditionalEffect> { public SkeletonAdditionalEffect Create() => new(); }
    public class WillReviveFactory : IFactory<WillRevive> { public WillRevive Create() => new(); }
    public class CoinStarSilverFactory : IFactory<CoinStarSilver> { public CoinStarSilver Create() => new(); }
    public class CoinStarGoldenFactory : IFactory<CoinStarGolden> { public CoinStarGolden Create() => new(); }
    public class PouchGreenFactory : IFactory<PouchGreen> { public PouchGreen Create() => new(); }
    public class PouchRedFactory : IFactory<PouchRed> { public PouchRed Create() => new(); }
    public class ChestGoldFactory : IFactory<ChestGold> { public ChestGold Create() => new(); }
}   