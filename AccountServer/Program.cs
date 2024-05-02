using AccountServer;
using AccountServer.DB;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConfigService>();

var configService = new ConfigService();
var path = Environment.GetEnvironmentVariable("CONFIG_PATH") ??
           "/Users/jwy/Documents/Dev/CryWolf/Config/CryWolfAccountConfig.json";
var appConfig = configService.LoadGoogleConfigs(path);

// Add services to the container.
// -- StartUp.cs
var defaultConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                              builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = appConfig.GoogleClientId;
        options.ClientSecret = appConfig.GoogleClientSecret;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(defaultConnectionString, new MariaDbServerVersion(new Version(10, 11, 2)));
});

// -- StartUp.cs - Configure
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

Console.WriteLine("test1");
// Check DB Connection
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.OpenConnection();
        dbContext.Database.CloseConnection();
        Console.WriteLine("DB Connection Success");
    }
    catch (Exception e)
    {
        Console.WriteLine($"DB Connection failed: {e.Message}");
    }
}

Console.WriteLine("test2");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();