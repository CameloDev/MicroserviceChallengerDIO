
using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using RabbitMQ.Client;
using VendasService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VendasDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("MySqlConnection")
    ?? throw new InvalidOperationException("Connection string 'MySqlConnection' not found.")));

builder.Services.AddSingleton<Task<IConnection>>(async sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"] ?? throw new InvalidOperationException("esta faltado Hostname no appsettings.json"),
        UserName = builder.Configuration["RabbitMQ:UserName"] ?? throw new InvalidOperationException("esta faltado Username no appsettings.json"),
        Password = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("esta faltado Password no appsettings.json"),
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        ClientProvidedName = builder.Environment.ApplicationName
    };
    
    return await factory.CreateConnectionAsync();
});
builder.Services.AddSingleton<Task<IChannel>>(async sp =>
{
    var connection = await sp.GetRequiredService<Task<IConnection>>();
    return await connection.CreateChannelAsync();
});

builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();