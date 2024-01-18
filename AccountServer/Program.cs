using AccountServer.DB;
using Microsoft.EntityFrameworkCore;
using SharedDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var sharedConnectionString = builder.Configuration.GetConnectionString("SharedConnection");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(defaultConnectionString, new MariaDbServerVersion(new Version(10, 11, 2)));
});

builder.Services.AddDbContext<SharedDbContext>(options =>
{
    options.UseMySql(defaultConnectionString, new MariaDbServerVersion(new Version(10, 11, 2)));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseAuthorization();

app.MapControllers();

app.Run();