using Microsoft.EntityFrameworkCore;

namespace SharedDB;

public class SharedDbContext : DbContext
{
    public DbSet<TokenDb> Tokens { get; set; }
    public DbSet<ServerDb> Servers { get; set; }

    // GameServer
    public SharedDbContext()
    {
        
    }
    
    // ASP.NET
    public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
    {
        
    }
    
    // GameServer
    public static string ConnectionString { get; set; } = "Server=localhost;Port=3306;Database=SharedDB;Uid=jwy;Pwd=7628;";
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (options.IsConfigured == false)
        {
            options.UseMySql(ConnectionString, new MariaDbServerVersion(new Version(10, 11, 2)));
        }    
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<TokenDb>().HasIndex(token => token.UserId).IsUnique();
        builder.Entity<ServerDb>().HasIndex(server => server.Name).IsUnique();
    }
}