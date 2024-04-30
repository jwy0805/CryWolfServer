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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();