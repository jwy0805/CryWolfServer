using Google.Protobuf.Protocol;

namespace Server.Game.AI;

public sealed class AiBlackboard
{
    public Faction Faction { get; set; }
    
    public int Resource;
    public int NpcBaseLevel;
    public int PlayerBaseLevel;
    public int RoundTimeLeft;
    
    public int PopulationPerKind;
    public int NpcMaxPopulation;
    public int NpcPopulation;
    public int PlayerMaxPopulation;
    public int PlayerPopulation;

    public float UnitProb; // 근거리, 원거리 유닛 비율 -> 1에 가까울수록 균형 
    
    // For Pressure System
    public float TotalPressure; // -1 ~ 1 이 일반적, 그 이상은 극단적 상황
    public readonly float RangedFactor = 0.1f;
    public readonly float MeleeFactor = 0.05f;
    public readonly float TimeFactor = 0.05f;
    public readonly float FenceFactor = 0.1f;
    public readonly float PopDiffThreshold = 4f;
    public readonly float RoundTimeLeftFactor = 0.3f;
    public readonly float SkillCostLimit = 0.75f;
    
    // For Method: PickUnitToUpgrade
    public readonly double SupporterProbMin = 0.07;
    public readonly double SupporterProbMax = 0.16;
    public readonly double JitterScale = 0.05; // 소량의 무작위성
    public readonly double TempSoftmax = 0.9; // Softmax temperature: 낮을수록 결정적
    public readonly double EpsilonExplore = 0.05; // epsilon-greedy exploration: 가끔 완전 랜덤
    public double WaveOverflowFactor { get; set; }
    public double FenceDamageFactor { get; set; }
    public double FenceLowDamageFactor { get; set; }
    public double FenceMoveFactor { get; set; }
    public double AffinityFactor { get; set; }
    public double AffinityFloorFactor { get; set; } // 상성으로 인한 과도한 급락 방지 하한 배율
    
    // For Sheep AI
    public bool AntiAircraft;
    public bool ProtectFenceInside;

    public Dictionary<UnitId, int> MyCounts = new();
    public Dictionary<UnitId, int> EnemyCounts = new();

    public Dictionary<Skill, bool> SkillReady = new();
}