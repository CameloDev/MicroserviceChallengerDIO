using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;
using EstoqueService.Services;
using RabbitMQ.Client;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EstoqueContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("EstoqueConnection")
    ?? throw new InvalidOperationException("Connection string 'EstoqueConnection' not found.")));

var factory = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMQ:HostName"]
        ?? throw new InvalidOperationException("Está faltando Hostname no appsettings.json"),
    UserName = builder.Configuration["RabbitMQ:UserName"]
        ?? throw new InvalidOperationException("Está faltando Username no appsettings.json"),
    Password = builder.Configuration["RabbitMQ:Password"]
        ?? throw new InvalidOperationException("Está faltando Password no appsettings.json"),
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    ClientProvidedName = builder.Environment.ApplicationName,
};

var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton<IConnection>(connection);
builder.Services.AddHostedService<AtualizarEstoqueConsumer>();
var channel = await connection.CreateChannelAsync();
builder.Services.AddSingleton<IChannel>(channel);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMvc();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EstoqueService", Version = "v1" });
});

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
