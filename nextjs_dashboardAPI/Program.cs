using Dapper;
using Microsoft.Data.SqlClient;
using nextjs_dashboardAPI.Services;
using StackExchange.Redis;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// 從 JSON 讀取 DashboardDatabase 配置
var dbConfig = builder.Configuration.GetSection("DashboardDatabase");
var user = dbConfig["User"];
var password = dbConfig["Password"];
var server = dbConfig["Server"];
var port = dbConfig["Port"];
var database = dbConfig["Database"];
var encrypt = dbConfig.GetSection("Options")["Encrypt"] ?? "false";
var trustCert = dbConfig.GetSection("Options")["TrustServerCertificate"] ?? "true";

// 組成 MSSQL 連線字串
var connectionString = $"Server={server},{port};Database={database};User Id={user};Password={password};Encrypt={encrypt};TrustServerCertificate={trustCert};";

// 註冊 IDbConnection (Transient 每次取得新連線)
builder.Services.AddTransient<IDbConnection>(sp => new SqlConnection(connectionString));

// 註冊 InvoiceService (Scoped 每個 HTTP Request 使用同一個實例)
builder.Services.AddScoped<InvoiceService>();

// Add controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 註冊 Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // 前端網址
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

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
