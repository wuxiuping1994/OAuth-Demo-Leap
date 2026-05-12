using OAuthDemoLeap.Models;
using OAuthDemoLeap.Services.PkceService;
using OAuthDemoLeap.Services.TokenExchangeService;
using OAuthDemoLeap.Services.TokenValidationService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IPkceService, PkceService>();
builder.Services.AddHttpClient<ITokenExchangeService, TokenExchangeService>();
builder.Services.AddHttpClient<ITokenValidationService, TokenValidationService>();

builder.Services.Configure<OAuthConfiguration>(
    builder.Configuration.GetSection("OAuthConfiguration"));


// Required by AddSession() as the backing store for session data
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSession();
app.MapControllers();

app.Run();
