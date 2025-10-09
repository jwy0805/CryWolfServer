namespace Server.Game.AI;

public sealed class AiPolicy
{
    public int DecisionCooldownMs = 500;

    public readonly float UpkeepTolerance = 0.1f;
    
    // For Pressure System
    public float RangedFactor { get; init; } = 0.1f;
    public float MeleeFactor { get; init; } = 0.05f;
    public float TimeFactor { get; init; } = 0.05f;
    public float FenceFactor { get; init; } = 0.1f;
    public float PopDiffThreshold { get; init; } = 4f;
    public float RoundTimeLeftFactor { get; init; } = 0.3f;
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
}