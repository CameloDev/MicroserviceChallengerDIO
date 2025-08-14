using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using RabbitMQ.Client;
using VendasService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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