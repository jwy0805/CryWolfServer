using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public sealed class AiBlackboard
{
    public Player MyPlayer { get; set; }
    public Faction MyFaction { get; set; }
    public UnitId[] MyUnits { get; set; }
    public int RoundTimeLeft { get; set; }

    public int MyResource { get; set; }
    public int EnemyResource { get; set; }
    public int MyBaseLevel { get; set; }
    public int EnemyBaseLevel { get; set; }
    
    public int PopulationPerKind { get; set; }
    public int MyMaxPop { get; }
    public int MyPop { get; }
    public int EnemyMaxPop { get; }
    public int EnemyPop { get; }

    public double UnitProb { get; set; } // 근거리, 원거리 유닛 비율 -> 둘다 0이거나 동수면 0.5, 근거리 많을수록 1, 원거리 많을수록 0
    public double TotalPressure { get; set; } // -1 ~ 1 이 일반적, 그 이상은 극단적 상황
    
    public IReadOnlyDictionary<UnitId, int> MyCounts { get; }
    public IReadOnlyDictionary<UnitId, int> EnemyCounts { get; }
    public IReadOnlyDictionary<Skill, bool> SkillReady { get; }
    
    public AiPolicy Policy { get; set; }

    public AiBlackboard(Player player, Faction faction, UnitId[] myUnits, int roundTimeLeft, int myResource, int myBaseLevel, int myMaxPop, int myPop,
        IReadOnlyDictionary<UnitId, int> myCounts, int enemyResource, int enemyBaseLevel, int enemyMaxPop, int enemyPop,
        IReadOnlyDictionary<UnitId, int> enemyCounts, IReadOnlyDictionary<Skill, bool> skillReady, float unitProb,
        AiPolicy policy)
    {
        MyPlayer = player;
        MyFaction = faction;
        MyUnits = myUnits;
        RoundTimeLeft = roundTimeLeft;
        
        MyResource = myResource;
        MyBaseLevel = myBaseLevel;
        MyMaxPop = myMaxPop;
        MyPop = myPop;
        EnemyResource = enemyResource;
        EnemyBaseLevel = enemyBaseLevel;
        EnemyMaxPop = enemyMaxPop;
        EnemyPop = enemyPop;
        
        MyCounts = myCounts;
        EnemyCounts = enemyCounts;
        
        SkillReady = skillReady;
        UnitProb = unitProb;
        Policy = policy;
    }
}