using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// 只把 /gateway 前缀交给 Ocelot
app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/gateway"),
    gatewayApp =>
    {
        gatewayApp.UseOcelot().GetAwaiter().GetResult();
    });

app.Run();