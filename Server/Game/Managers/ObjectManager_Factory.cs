using Google.Protobuf.Protocol;
using Server.Game.etc;
using Server.Game.Object.etc;
using Server.Game.Resources;

namespace Server.Game;

public sealed partial class ObjectManager
{
    private readonly Dictionary<UnitId, ITowerFactory> _towerDict = new()
    {
        { UnitId.Bud, new BudFactory() },
        { UnitId.Bloom, new BloomFactory() },
        { UnitId.Blossom, new BlossomFactory() },
        { UnitId.PracticeDummy, new PracticeDummyFactory() },
        { UnitId.TargetDummy, new TargetDummyFactory() },
        { UnitId.TrainingDummy, new TrainingDummyFactory() },
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
        { UnitId.WolfPup, new WolfPupFactory() },
        { UnitId.Wolf, new WolfFactory() },
        { UnitId.Werewolf, new WerewolfFactory() },
        { UnitId.Lurker, new LurkerFactory() },
        { UnitId.Creeper, new CreeperFactory() },
        { UnitId.Horror, new HorrorFactory() },
        { UnitId.Shell, new ShellFactory() },
        { UnitId.Spike, new SpikeFactory() },
        { UnitId.Hermit, new HermitFactory() },
        { UnitId.Snakelet, new SnakeletFactory() },
        { UnitId.Snake, new SnakeFactory() },
        { UnitId.SnakeNaga, new SnakeNagaFactory() },
        { UnitId.MosquitoBug, new MosquitoBugFactory() },
        { UnitId.MosquitoPester, new MosquitoPesterFactory() },
        { UnitId.MosquitoStinger, new MosquitoStingerFactory() },
    };
    
    private readonly Dictionary<ProjectileId, IProjectileFactory> _projectileDict = new()
    {
        { ProjectileId.BasicProjectile, new BasicAttackFactory() },
        { ProjectileId.SmallFire, new SmallFireFactory() },
        { ProjectileId.BigFire, new BigFireFactory() },
        { ProjectileId.PoisonProjectile, new PoisonAttackFactory() },
        { ProjectileId.BigPoison, new BigPoisonFactory() },
        { ProjectileId.SeedProjectile, new SeedFactory() },
        { ProjectileId.BlossomSeed, new BlossomSeedFactory() },
        { ProjectileId.BlossomProjectile, new BlossomArrowFactory() },
        { ProjectileId.HauntProjectile, new HauntArrowFactory() },
        { ProjectileId.HauntFireProjectile, new HauntFireAttackFactory() },
        { ProjectileId.SoulMageProjectile, new SoulMageAttackFactory() },
        { ProjectileId.SunfloraPixieProjectile, new SunfloraPixieArrowFactory() },
        { ProjectileId.SunfloraPixieFire, new SunfloraPixieFireFactory() },
        { ProjectileId.MothMoonAttack, new MothMoonAttackFactory()},
        { ProjectileId.MothCelestialPoisonProjectile, new MothCelestialPoisonAttackFactory() },
        { ProjectileId.MosquitoStingerProjectile, new MosquitoStingerAttackFactory() },
        { ProjectileId.SpikeProjectile, new SpikeArrowFactory() },
        { ProjectileId.HermitProjectile, new HermitArrowFactory() }
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
        { EffectId.Upgrade, new EffectFactory() }
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
    
    public class ShellFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Shell();
    }
    
    public class SpikeFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Spike();
    }
    
    public class HermitFactory : IMonsterFactory
    {
        public Monster CreateMonster() => new Hermit();
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
    
    public class BasicAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BasicAttack();
    }
    
    public class SmallFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SmallFire();
    }
    
    public class BigFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BigFire();
    }
    
    public class PoisonAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new PoisonAttack();
    }
    
    public class BigPoisonFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BigPoison();
    }
    
    public class SeedFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new Seed();
    }
    
    public class BlossomSeedFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BlossomSeed();
    }
    
    public class BlossomArrowFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new BlossomArrow();
    }
    
    public class HauntArrowFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HauntArrow();
    }
    
    public class HauntFireAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HauntFireAttack();
    }
    
    public class SoulMageAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SoulMageAttack();
    }
    
    public class SunfloraPixieArrowFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SunfloraPixieArrow();
    }
    
    public class SunfloraPixieFireFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SunfloraPixieFire();
    }
    
    public class MothMoonAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MothMoonAttack();
    }
    
    public class MothCelestialPoisonAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MothCelestialPoisonAttack();
    }
    
    public class MosquitoStingerAttackFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new MosquitoStingerAttack();
    }

    public class SpikeArrowFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new SpikeArrow();
    }

    public class HermitArrowFactory : IProjectileFactory
    {
        public Projectile CreateProjectile() => new HermitArrow();
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