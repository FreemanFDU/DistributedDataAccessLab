using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ 数据库
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite("Data Source=products.db"));

// ✅ 注册 RabbitMQ Consumer（考试必须）
builder.Services.AddHostedService<RabbitMqConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ 自动创建数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();