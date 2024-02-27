using System.ComponentModel.DataAnnotations.Schema;

namespace AccountServer.DB;

#region Enum

public enum UserRole
{
    Admin,
    User
}

public enum UserState
{
    Activate,
    Deactivate,
    Suspension,
    Ban
}

public enum Class
{
    Commoner,
    Squire,
    Knight,
    HighRankingKnight,
    Baron,
    Count,
    Duke,
}

public enum UnitRole
{
    Warrior,
    Dealer,
    Mage,
    Supporter
}

public enum Camp
{
    Sheep,
    Wolf
}

#endregion

[Table("User")]
public class User
{
    public int UserId { get; set; }
    public string Password { get; set; }
    public string UserName { get; set; }
    public UserRole Role { get; set; }
    public string Email { get; set; }
    public UserState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RankPoint { get; set; }
    public int Gold { get; set; }
    public int Gem { get; set; }
}

[Table("Unit")]
public class Unit
{
    public int UnitId { get; set; }
    public string UnitName { get; set; }
    public UnitRole Role { get; set; }
    public Class Class { get; set; }
    public int Stage { get; set; }
}

[Table("Deck")]
public class Deck
{
    public int DeckId { get; set; }
    public int UserId { get; set; }
    public Camp Camp { get; set; }
}

[Table("Deck_Unit")]
public class DeckUnit
{
    public int DeckId { get; set; }
    public int UnitId { get; set; }
}

[Table("User_Unit")]
public class UserUnit
{
    public int UserId { get; set; }
    public int UnitId { get; set; }
    public int Count { get; set; }
}

