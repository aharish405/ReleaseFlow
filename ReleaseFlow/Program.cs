using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Authorization;
using ReleaseFlow.Data;
using ReleaseFlow.Models;
using ReleaseFlow.Repositories;
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
        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.Requirements.Add(new RoleRequirement(RoleNames.SuperAdmin)));
    
    options.AddPolicy("DeployerOrAbove", policy =>
        policy.Requirements.Add(new RoleRequirement(RoleNames.SuperAdmin, RoleNames.Deployer)));
    
    options.AddPolicy("Authenticated", policy =>
        policy.Requirements.Add(new RoleRequirement(RoleNames.SuperAdmin, RoleNames.Deployer, RoleNames.ReadOnly)));
});

builder.Services.AddScoped<IAuthorizationHandler, RoleHandler>();
builder.Services.AddHttpContextAccessor();

// Register HttpClient for health checks
builder.Services.AddHttpClient();

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIISSiteService, IISSiteService>();
builder.Services.AddScoped<IIISAppPoolService, IISAppPoolService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddScoped<IRollbackService, RollbackService>();

// Configure file upload size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 524288000; // 500 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.Initialize(context);
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

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

