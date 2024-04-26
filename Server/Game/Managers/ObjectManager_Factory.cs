using Google.Protobuf.Protocol;
using Server.Game.etc;
using Server.Game.Object.etc;
using Server.Game.Resources;

namespace Server.Game;

public sealed partial class ObjectManager
{
    private readonly Dictionary<UnitId, ITowerFactory> _towerDict = new()
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

    private readonly Dictionary<UnitId, IMonsterFactory> _monsterDict = new()
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
    
    private readonly Dictionary<ProjectileId, IProjectileFactory> _projectileDict = new()
    {
        { ProjectileId.BasicProjectile, new BasicProjectileFactory() },
        { ProjectileId.SmallFire, new SmallFireFactory() },
        { ProjectileId.BigFire, new BigFireFactory() },
        { ProjectileId.SmallPoison, new SmallPoisonFactory() },
        { ProjectileId.BigPoison, new BigPoisonFactory() },
        { ProjectileId.SeedProjectile, new SeedProjectileFactory() },
        { ProjectileId.BlossomSeed, new BlossomSeedFactory() },
        { ProjectileId.BlossomProjectile, new BlossomProjectileFactory() },
        { ProjectileId.HauntProjectile, new HauntProjectileFactory() },
        { ProjectileId.HauntFire, new HauntFireProjectileFactory() },
        { ProjectileId.SoulMageProjectile, new SoulMageProjectileFactory() },
        { ProjectileId.SunfloraPixieProjectile, new SunfloraPixieProjectileFactory() },
        { ProjectileId.SunfloraPixieFire, new SunfloraPixieFireFactory() },
        { ProjectileId.MothMoonProjectile, new MothMoonProjectileFactory()},
        { ProjectileId.MothCelestialPoison, new MothCelestialPoisonProjectileFactory() },
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
        { ProjectileId.RabbitAggro, new RabbitAggroFactory() }
    };

    private readonly Dictionary<EffectId, IEffectFactory> _effectDict = new()
    {
        { EffectId.LightningStrike, new LightningStrikeFactory() },
        { EffectId.PoisonBelt, new PoisonBeltFactory() },
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
        { EffectId.Upgrade, new EffectFactory() },
        { EffectId.SkeletonGiantEffect, new SkeletonGiantEffectFactory()},
        { EffectId.SkeletonGiantSkill, new SkeletonGiantSkill() },
        { EffectId.SkeletonGiantRevive, new SkeletonGiantRevive() }
    };

    private readonly Dictionary<ResourceId, IResourceFactory> _resourceDict = new()
    {
        { ResourceId.CoinStarSilver, new CoinStarSilverFactory() },
        { ResourceId.CoinStarGolden, new CoinStarGoldenFactory() },
        { ResourceId.PouchGreen, new PouchGreenFactory() },
        { ResourceId.PouchRed, new PouchRedFactory() },
        { ResourceId.ChestGold, new ChestGoldFactory() }
    };
    
    public interface ITowerFactory
    {
        Tower CreateTower();
    }
    
    public interface IMonsterFactory
    {
        Monster CreateMonster();
    }
    
    public interface IProjectileFactory
    {
        Projectile CreateProjectile();
    }
    
    public interface IEffectFactory
    {
        Effect CreateEffect();
    }
    
    public interface IResourceFactory
    {
        Resource CreateResource();
    }

    public class MonsterStatueFactory
    {
        public MonsterStatue CreateStatue() => new();
    }

    public class TuskFactory
    {
        public Tusk CreateTusk() => new();
    }
    
    public class FenceFactory
    {
        public Fence CreateFence() => new();
    }
    
    public class BunnyFactory : ITowerFactory
    {
        public Tower CreateTower() => new Bunny();
    }
    
    public class RabbitFactory : ITowerFactory
    {
        public Tower CreateTower() => new Rabbit();
    }
    
    public class HareFactory : ITowerFactory
    {
        public Tower CreateTower() => new Hare();
    }
    
    public class MushroomFactory : ITowerFactory
    {
        public Tower CreateTower() => new Mushroom();
    }
    
    public class FungiFactory : ITowerFactory
    {
        public Tower CreateTower() => new Fungi();
    }
    
    public class ToadstoolFactory : ITowerFactory
    {
        public Tower CreateTower() => new Toadstool();
    }
    
    public class SeedFactory : ITowerFactory
    {
        public Tower CreateTower() => new Seed();
    }
    
    public class SproutFactory : ITowerFactory
    {
        public Tower CreateTower() => new Sprout();
    }
    
    public class FlowerPotFactory : ITowerFactory
    {
        public Tower CreateTower() => new FlowerPot();
    }
    
    public class BudFactory : ITowerFactory
    {
        public Tower CreateTower() => new Bud();
    }
    
    public class BloomFactory : ITowerFactory
    {
        public Tower CreateTower() => new Bloom();
    }
    
    public class BlossomFactory : ITowerFactory
    {
        public Tower CreateTower() => new Blossom();
    }
    
    public class PracticeDummyFactory : ITowerFactory
    {
        public Tower CreateTower() => new PracticeDummy();
    }
    
    public class TargetDummyFactory : ITowerFactory
    {
        public Tower CreateTower() => new TargetDummy();
    }
    
    public class TrainingDummyFactory : ITowerFactory
    {
        public Tower CreateTower() => new TrainingDummy();
    }
    
    public class ShellFactory : ITowerFactory
    {
        public Tower CreateTower() => new Shell();
    }
    
    public class SpikeFactory : ITowerFactory
    {
        public Tower CreateTower() => new Spike();
    }
    
    public class HermitFactory : ITowerFactory
    {
        public Tower CreateTower() => new Hermit();
    }
    
    public class SunBlossomFactory : ITowerFactory
    {
        public Tower CreateTower() => new SunBlossom();
    }
    
    public class SunflowerFairyFactory : ITowerFactory
    {
        public Tower CreateTower() => new SunflowerFairy();
    }
    
    public class SunfloraPixieFactory : ITowerFactory
    {
        public Tower CreateTower() => new SunfloraPixie();
    }
    
    public class MothLunaFactory : ITowerFactory
    {
        public Tower CreateTower() => new MothLuna();
    }
    
    public class MothMoonFactory : ITowerFactory
    {
        public Tower CreateTower() => new MothMoon();
    }
    
    public class MothCelestialFactory : ITowerFactory
    {
        public Tower CreateTower() => new MothCelestial();
    }
    
    public class SoulFactory : ITowerFactory
    {
        public Tower CreateTower() => new Soul();
    }
    
    public class HauntFactory : ITowerFactory
    {
        public Tower CreateTower() => new Haunt();
    }
    
    public class SoulMageFactory : ITowerFactory
    {
        public Tower CreateTower() => new SoulMage();
    }

    public class PumpkinFactory : ITowerFactory
    {
        public Tower CreateTower() => new Pumpkin();
    }
    
    public class DogPupFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new DogPup();
    }
    
    public class DogBarkFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new DogBark();
    }
    
    public class DogBowwowFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new DogBowwow();
    }
    
    public class BurrowFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Burrow();
    }
    
    public class MoleRatFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new MoleRat();
    }
    
    public class MoleRatKingFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new MoleRatKing();
    }
    
    public class MosquitoBugFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new MosquitoBug();
    }
    
    public class MosquitoPesterFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new MosquitoPester();
    }
    
    public class MosquitoStingerFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new MosquitoStinger();
    }
    
    public class WolfPupFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new WolfPup();
    }
    
    public class WolfFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Wolf();
    }
    
    public class WerewolfFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Werewolf();
    }

    public class BombFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Bomb();
    }
    
    public class SnowBombFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new SnowBomb();
    }
    
    public class PoisonBombFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new PoisonBomb();
    }
    
    public class CactiFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Cacti();
    }
    
    public class CactusFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Cactus();
    }
    
    public class CactusBossFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new CactusBoss();
    }
    
    public class LurkerFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Lurker();
    }
    
    public class CreeperFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Creeper();
    }
    
    public class HorrorFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Horror();
    }
    
    public class SnakeletFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Snakelet();
    }
    
    public class SnakeFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Snake();
    }
    
    public class SnakeNagaFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new SnakeNaga();
    }
    
    public class SkeletonFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Skeleton();
    }
    
    public class SkeletonGiantFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new SkeletonGiant();
    }
    
    public class SkeletonMageFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new SkeletonMage();
    }
    
    public class BasicProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BasicProjectile();
    }
    
    public class SmallFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SmallFire();
    }
    
    public class BigFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BigFire();
    }
    
    public class SmallPoisonFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SmallPoison();
    }
    
    public class BigPoisonFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BigPoison();
    }
    
    public class SeedProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SeedProjectile();
    }
    
    public class BlossomSeedFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BlossomSeed();
    }
    
    public class BlossomProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BlossomProjectile();
    }
    
    public class HauntProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HauntProjectile();
    }
    
    public class HauntFireProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HauntFire();
    }
    
    public class SoulMageProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SoulMageProjectile();
    }
    
    public class SunfloraPixieProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SunfloraPixieProjectile();
    }
    
    public class SunfloraPixieFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SunfloraPixieFire();
    }
    
    public class MothMoonProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MothMoonProjectile();
    }
    
    public class MothCelestialPoisonProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MothCelestialPoison();
    }
    
    public class MosquitoStingerProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MosquitoStingerProjectile();
    }

    public class SpikeProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SpikeProjectile();
    }

    public class HermitProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HermitProjectile();
    }
    
    public class MosquitoPesterProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MosquitoPesterProjectile();
    }
    
    public class BombProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BombProjectile();
    }

    public class BombSkillFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BombSkill();
    }
    
    public class SnowBombSkillFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SnowBombSkill();
    }
    
    public class PoisonBombSkillFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new PoisonBombSkill();
    }
    
    public class SkeletonMageProjectileFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SkeletonMageProjectile();
    }
    
    public class BasicProjectile2Factory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BasicProjectile2();
    }
    
    public class HarePunchFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HarePunch();
    }
    
    public class RabbitAggroFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new RabbitAggro();
    }
    
    public class LightningStrikeFactory : IEffectFactory
    {
        public Effect CreateEffect() => new LightningStrike();
    }
    
    public class PoisonBeltFactory : IEffectFactory
    {
        public Effect CreateEffect() => new PoisonBelt();
    }
    
    public class HolyAuraFactory : IEffectFactory
    {
        public Effect CreateEffect() => new HolyAura();
    }
    
    public class SoulMagePunchFactory : IEffectFactory
    {
        public Effect CreateEffect() => new SoulMagePunch();
    }

    public class MeteorFactory : IEffectFactory
    {
        public Effect CreateEffect() => new Meteor();
    }

    public class StateSlowFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateSlow();
    }

    public class StatePoisonFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StatePoison();
    }
    
    public class StateFaintFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateFaint();
    }

    public class StateCurseFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateCurse();
    }

    public class StateBurnFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateBurn();
    }
    
    public class StateHealFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateHeal();
    }

    public class StateAggroFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateAggro();
    }

    public class StateDebuffRemoveFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StateDebuffRemove();
    }

    public class GreenGateFactory : IEffectFactory
    {
        public Effect CreateEffect() => new GreenGate();
    }

    public class NaturalTornadoFactory : IEffectFactory
    {
        public Effect CreateEffect() => new NaturalTornado();
    }

    public class PurpleBeamFactory : IEffectFactory
    {
        public Effect CreateEffect() => new PurpleBeam();
    }
    
    public class StarFallFactory : IEffectFactory
    {
        public Effect CreateEffect() => new StarFall();
    }

    public class HorrorRollFactory : IEffectFactory
    {
        public Effect CreateEffect() => new HorrorRoll();
    }
    
    public class EffectFactory : IEffectFactory
    {
        public Effect CreateEffect() => new Upgrade();
    }
    
    public class SkeletonGiantEffectFactory : IEffectFactory
    {
        public Effect CreateEffect() => new SkeletonGiantEffect();
    }

    public class SkeletonGiantSkill : IEffectFactory
    {
        public Effect CreateEffect() => new Game.SkeletonGiantSkill();
    }

    public class SkeletonGiantRevive : IEffectFactory
    {
        public Effect CreateEffect() => new Game.SkeletonGiantRevive();
    }

    public class CoinStarSilverFactory : IResourceFactory
    {
        public Resource CreateResource() => new CoinStarSilver();
    }
    
    public class CoinStarGoldenFactory : IResourceFactory
    {
        public Resource CreateResource() => new CoinStarGolden();
    }
    
    public class PouchGreenFactory : IResourceFactory
    {
        public Resource CreateResource() => new PouchGreen();
    }
    
    public class PouchRedFactory : IResourceFactory
    {
        public Resource CreateResource() => new PouchRed();
    }
    
    public class ChestGoldFactory : IResourceFactory
    {
        public Resource CreateResource() => new ChestGold();
    }
}