using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Apenas Ocelot + SwaggerForOcelot
builder.Services.AddOcelot(builder.Configuration);
 //builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

/* Swagger unificado
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});
*/

await app.UseOcelot();

app.Run();
