using Microsoft.EntityFrameworkCore;

namespace AccountServer.DB;

public class AppDbContext : DbContext
{
    public DbSet<User> User { get; set; }
    public DbSet<UserUnit> UserUnit { get; set; }
    public DbSet<Deck> Deck { get;set; }
    public DbSet<DeckUnit> DeckUnit { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(user => user.UserName).IsUnique();
        builder.Entity<DeckUnit>().HasKey(deckUnit => new { deckUnit.DeckId, deckUnit.UnitId });
        builder.Entity<UserUnit>().HasKey(userUnit => new { userUnit.UserId, userUnit.UnitId });
    }
}