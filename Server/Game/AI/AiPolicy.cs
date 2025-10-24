namespace Server.Game.AI;

public sealed class AiPolicy
{
    private readonly Random _random = new();
    
    public int DecisionCooldownMs = 500;

    public readonly float UpkeepTolerance = 0.1f;
    public readonly double UnitRoleProb = 0.4;
    public double ValueDiffThreshold = 0.9; // For Economic Upgrade
    public readonly double IdleThreshold = 1.2;
    public readonly double SheepDamagedThreshold = 0.2; // For Sheep AI
    public readonly double UrgentSpawnValue = 10;
    
    // For Pressure System
    public float RangedFactor { get; init; } = 0.1f;
    public float MeleeFactor { get; init; } = 0.05f;
    public float TimeFactor { get; init; } = 0.05f;
    public float FenceFactor { get; init; } = 0.1f;
    public float PopDiffThreshold { get; init; } = 4f;
    public float RoundTimeLeftFactor { get; init; } = 0.9f;
    public float SkillCostLimit { get; init; } = 0.75f;
    
    // For Method: PickUnitToUpgrade
    public double SupporterProbMin { get; init; } = 0.07;
    public double SupporterProbMax { get; init; } = 0.16;
    public double JitterScale { get; init; } = 0.05; // 소량의 무작위성
    public double TempSoftmax { get; init; } = 0.9; // Softmax temperature: 낮을수록 결정적
    public double EpsilonExplore { get; init; } = 0.05; // epsilon-greedy exploration: 가끔 완전 랜덤
    
    public double WaveOverflowFactor { get; init; } = 1.2;
    public double FenceDamageFactor { get; init; } = 1.3;
    public double FenceLowDamageFactor { get; init; } = 2.0;
    public double FenceMoveFactor { get; init; } = 1.1;
    public double AffinityFactor { get; init; } = 0.9;
    public double AffinityFloorFactor { get; init; } = 0.25; // 상성으로 인한 과도한 급락 방지 하한 배율
    
    // ai 계산식 모음
    public double CalcPressureByFence(float fenceZ) => (fenceZ - (-10)) / 4f * FenceFactor;
    public double CalcPressureByValue(int myValue, int enemyValue) 
        => enemyValue == 0 ? 0 : Math.Max(enemyValue - myValue, 0) / (double)enemyValue * 8; 
    public double CalcCurrentPressure(int rangedDiff, int meleeDiff) 
        => rangedDiff * RangedFactor + meleeDiff * MeleeFactor;
    public double ComparePopulation(int enemyPop, int pop) => Math.Pow((enemyPop / (double)pop), 2) / Math.PI;
    public double EvaluatePopulation(int popDiff) => (Math.Pow(2, popDiff) / 50);
    public double CalcEconomicUpgrade(double min, double max) 
        => (float)Util.Util.GetRandomValueByGaussian(_random, min, max, 0, 1);
    public double CompareValueForUnitUpgrade(int myValue, int enemyValue, int myPop)
        => enemyValue == 0 ? 0 : Math.Max(enemyValue - myValue, 0) / (double)enemyValue * 8;
        
}