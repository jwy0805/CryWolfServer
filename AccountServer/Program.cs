using AccountServer;
using AccountServer.DB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var isLocal = Environment.GetEnvironmentVariable("ENVIRONMENT") == "Local";
if (isLocal == false)
{   // Kestrel Server
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(443, listenOptions =>
        {
            var certPath = Environment.GetEnvironmentVariable("CERT_PATH");
            var certPwd = Environment.GetEnvironmentVariable("CERT_PASSWORD");
            if (certPath != null && certPwd != null) listenOptions.UseHttps(certPath, certPwd);
            else throw new Exception("Certification path or password is null");
        });
    });
}

builder.Services.AddSingleton<ConfigService>();

var configService = new ConfigService();
var path = Environment.GetEnvironmentVariable("CONFIG_PATH") ??
           "/Users/jwy/Documents/Dev/CryWolf/Config/CryWolfAccountConfig.json";
var appConfig = configService.LoadGoogleConfigs(path);

// Add services to the container. -- StartUp.cs
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

// builder.Services.AddDbContext<AppDbContext>(options =>
// {
//     options.UseMySql(defaultConnectionString, new MariaDbServerVersion(new Version(10, 11, 2)));
// });

// -- StartUp.cs - Configure
if (isLocal == false)
{
#pragma warning disable CA1416
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
        .ProtectKeysWithDpapiNG();
#pragma warning restore CA1416
}

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// // Check DB Connection
// using (var scope = app.Services.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     try
//     {
//         dbContext.Database.OpenConnection();
//         dbContext.Database.CloseConnection();
//         Console.WriteLine("DB Connection Success");
//     }
//     catch (Exception e)
//     {
//         throw new Exception($"DB Connection failed: {e.Message}");
//     }
// }

Console.WriteLine("test");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();