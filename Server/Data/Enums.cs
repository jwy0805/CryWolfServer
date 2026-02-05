// ReSharper disable InconsistentNaming
namespace Server.Data;

public enum GameMode
{
    None,
    Rank,
    Friendly,
    Single,
    Tutorial,
    Test,
    AiSimulation,
}

public enum SkillType
{
    None = 0,
    Attack = 1,
    Defence = 2,
    Support = 3,
    Buff = 4,
    Debuff = 5,
    CrowdControl = 6,
    Base = 7,
    Main = 8,
}

public enum AttackType
{
    Ground = 0,
    Air = 1,
    Both = 2
}

public enum EventCounterKey
{
    friendly_match,
    first_purchase,
    single_play_win,
}

public enum EventRepeatType
{
    None,
    Daily,
    Weekly,
    Monthly,
}