using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SignalRChatServer.Controllers;
using SignalRChatServer.Data;
using SignalRChatServer.Hubs;
using SignalRChatServer.Services;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IPrivateMessageRepository, PrivateMessageRepository>();
builder.Services.AddSingleton<IAuthService, AuthService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5273", "https://localhost:5273")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// JWT Configuration
var jwtConfig = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtConfig["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig["Secret"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR Token Forwarding
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
    options.StreamBufferCapacity = 10;
});

var app = builder.Build();

// Middleware pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

#region CreateData
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//    try
//    {
//        await db.Database.ExecuteSqlRawAsync(@"
//            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'FileData')
//            BEGIN
//                ALTER TABLE [Messages] ADD 
//                    [FileData] varbinary(max) NULL,
//                    [FileName] nvarchar(max) NULL,
//                    [FileType] nvarchar(max) NULL
//                PRINT 'Columns added to Messages table'
//            END
//        ");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Error altering table: {ex.Message}");
//    }
//}
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//    try
//    {
//        // Удаляем и создаём таблицу заново
//        await db.Database.ExecuteSqlRawAsync(@"
//            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PrivateMessages')
//                DROP TABLE PrivateMessages;
                
//            CREATE TABLE PrivateMessages (
//                Id INT IDENTITY(1,1) PRIMARY KEY,
//                FromUserId NVARCHAR(450) NOT NULL,
//                FromUserName NVARCHAR(450) NOT NULL,
//                ToUserId NVARCHAR(450) NOT NULL,
//                Text NVARCHAR(MAX) NULL,
//                Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
//                FileData VARBINARY(MAX) NULL,
//                FileName NVARCHAR(MAX) NULL,
//                FileType NVARCHAR(255) NULL,
//                FOREIGN KEY (FromUserId) REFERENCES Users(Id) ON DELETE NO ACTION,
//                FOREIGN KEY (ToUserId) REFERENCES Users(Id) ON DELETE NO ACTION
//            );
//        ");

//        Console.WriteLine("Таблица PrivateMessages пересоздана");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Ошибка при пересоздании таблицы: {ex.Message}");
//    }
//}
#endregion

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
    endpoints.MapHub<PrivateChatHub>("/privateChatHub");
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Создаёт таблицы, если их нет
}

#region DeleteMessageModel
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//    try
//    {
//        Console.WriteLine("Автоматическая очистка сообщений...");
//        int deletedCount = await db.Messages.ExecuteDeleteAsync();
//        Console.WriteLine($"Удалено {deletedCount} сообщений. Этот код можно удалять.");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Ошибка при очистке: {ex.Message}");
//    }
//}
#endregion

app.Run();