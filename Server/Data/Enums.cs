namespace Server.Data;

public enum GameMode
{
    None,
    Rank,
    Friendly,
    Single,
    Tutorial,
    Test,
    AiTest,
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