using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using RabbitMQ.Client;
using VendasService.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var key = builder.Configuration["Jwt:Key"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("SomenteVendedor", p => p.RequireRole("Vendedor", "Admin"));
});

builder.Services.AddDbContext<VendasDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("MySqlConnection")
    ?? throw new InvalidOperationException("Connection string 'MySqlConnection' not found.")));

builder.Services.AddSingleton<Task<IConnection>>(async sp => 
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"] ?? throw new InvalidOperationException("Hostname não configurado"),
        UserName = builder.Configuration["RabbitMQ:UserName"] ?? throw new InvalidOperationException("Username não configurado"),
        Password = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("Password não configurado"),
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };
    var connection = await factory.CreateConnectionAsync();
    return connection;
});

builder.Services.AddSingleton<Task<IChannel>>(async sp =>
{
    var connectionTask = sp.GetRequiredService<Task<IConnection>>();
    var connection = await connectionTask;

    var channel = await connection.CreateChannelAsync();

    return channel;
});

builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMvc();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VendasService", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();