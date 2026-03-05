using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ 注册 SQLite 数据库
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite("Data Source=Data/payments.db"));

// ✅ 注册 RabbitMQ 消费者（后台服务）
builder.Services.AddHostedService<OrderCreatedConsumer>();

// ✅ 添加 Controller 支持
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ 确保 Data 目录存在 + 自动创建数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

    // 创建 Data 文件夹（Docker 内）
    Directory.CreateDirectory("Data");

    db.Database.EnsureCreated();
}

// ✅ 启用 Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ✅ 映射 Controller
app.MapControllers();

app.Run();