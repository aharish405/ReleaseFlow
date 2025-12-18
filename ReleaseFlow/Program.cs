using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ReleaseFlow.Data;
using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Models;
using ReleaseFlow.Services;
using ReleaseFlow.Services.Deployment;
using ReleaseFlow.Services.IIS;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File("logs/releaseflow-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions { TableName = "ReleaseFlow_Logs", AutoCreateSqlTable = true })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Database (ADO.NET)
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<SqlHelper>();

// Register Repositories
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IDeploymentRepository, DeploymentRepository>();
builder.Services.AddScoped<IDeploymentStepRepository, DeploymentStepRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Configure Authentication
// For development: use cookie authentication
// For production (IIS): use Windows Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Home/AccessDenied";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// Simple authorization - all authenticated users are admins
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

// Register HttpClient for health checks
builder.Services.AddHttpClient();



// Register Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIISSiteService, IISSiteService>();
builder.Services.AddScoped<IIISAppPoolService, IISAppPoolService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddScoped<IRollbackService, RollbackService>();
builder.Services.AddScoped<ReleaseFlow.Services.IIS.IIISDiscoveryService, ReleaseFlow.Services.IIS.IISDiscoveryService>();

// Configure file upload size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 524288000; // 500 MB
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 524288000;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 524288000;
});


var app = builder.Build();

// Initialize database


// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

Log.Information("ReleaseFlow application starting");

app.Run();

