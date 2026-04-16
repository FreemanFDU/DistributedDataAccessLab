using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register SQLite database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite("Data Source=Data/payments.db"));

// Register RabbitMQ consumers as background services
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<OrderCancelledConsumer>();

// Add controller support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure Data directory exists and database is created
Directory.CreateDirectory("Data");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.EnsureCreated();
}

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Map controllers
app.MapControllers();

app.Run();