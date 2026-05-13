using OAuthDemoLeap.Models;
using OAuthDemoLeap.Services.PkceService;
using OAuthDemoLeap.Services.TokenExchangeService;
using OAuthDemoLeap.Services.TokenValidationService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<IPkceService, PkceService>();
builder.Services.AddHttpClient<ITokenExchangeService, TokenExchangeService>();
builder.Services.AddHttpClient<ITokenValidationService, TokenValidationService>();

builder.Services.Configure<OAuthConfiguration>(
    builder.Configuration.GetSection("OAuthConfiguration"));


// Required by AddSession() as the backing store for session data
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();


var app = builder.Build();

app.UseHttpsRedirection();

app.UseSession();
app.MapControllers();

app.Run();
