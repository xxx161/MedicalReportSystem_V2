using MedicalReportSystem.Models;
using MedicalReportSystem.Models.Config;
using MedicalReportSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// 添加CORS策略（只配置一次）
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("http://10.10.1.46:8081", "http://10.10.1.46:8081/api/Reports/open", "http://10.10.1.46")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 如果需要支持Session/Cookie
    });

    // 如果需要AllowAll策略
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "MedicalReport.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

// Swagger配置 - 修复类型冲突
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "检验检查互认系统API",
        Version = "1.0",
        Contact = new OpenApiContact
        {
            Name = "技术支持",
        }
    });

    // 关键修复：自定义SchemaId，避免类型冲突
    c.CustomSchemaIds(type =>
    {
        // 处理嵌套类型，将+替换为_，避免冲突
        var fullName = type.FullName ?? type.Name;
        return fullName.Replace("+", "_");
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT授权头，格式: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
});

// 其他服务注册
builder.Services.AddScoped<GBaseService>(provider =>
{
    var config = builder.Configuration.GetSection("GBase");
    var service = new GBaseService();
    service.Connect(
        config["ConnectionString"],
        config["Username"],
        config["Password"]);
    return service;
});
builder.Services.AddScoped<PersonService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddScoped<FileDataStorageService>();
builder.Services.AddHostedService<FileCleanupService>();
builder.Services.AddSingleton<ThirdPartyConfigService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OpenGaussConnection")));
builder.Services.AddScoped<GBaseDbContext>();

var reminderSettings = new ReminderSettings();
builder.Configuration.GetSection("ReminderSettings").Bind(reminderSettings);
builder.Services.AddScoped<IDbConnection>(provider =>
    provider.GetRequiredService<GBaseDbContext>().CreateConnection());
builder.Services.AddHostedService<OracleSyncService>();
builder.Services.AddHttpClient<INewStandardService, NewStandardService>();
builder.Services.AddScoped<INewStandardService, NewStandardService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "检验检查互认系统API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Data")),
    RequestPath = "/Data"
});

// 配置页面保护
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/config.html"), app2 =>
{
    app2.Use(async (context, next) =>
    {
        if (!context.Request.Cookies.TryGetValue("AdminAuth", out var auth) || auth != "true")
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("无权访问配置页面");
            return;
        }
        await next();
    });
    app2.UseStaticFiles();
});

app.UseCors("ProductionPolicy"); // 只使用一个CORS策略
app.UseRouting();
app.UseSession();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles(); // 一般只需要一次
app.MapFallbackToFile("index.html");

app.Run();