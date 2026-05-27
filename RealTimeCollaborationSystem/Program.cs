using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Hubs;
using RealTimeCollaborationSystem.Services;
using RealTimeCollaborationSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDistributedMemoryCache();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var dataProtectionKeysPath = ResolveConfiguredPath(
    builder.Configuration["DataProtection:KeysPath"],
    builder.Environment,
    Path.Combine("App_Data", "DataProtectionKeys"));
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RTCS.Session";
    options.IdleTimeout = TimeSpan.FromHours(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string. Configure ConnectionStrings:DefaultConnection, for example with the ConnectionStrings__DefaultConnection environment variable.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddSingleton<UploadStorageService>();

var app = builder.Build();
var uploadStorage = app.Services.GetRequiredService<UploadStorageService>();
uploadStorage.EnsureStorageDirectories();

await app.RunDatabaseSetupAsync();

// Middleware
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

if (!uploadStorage.UsesWebRootStorage)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadStorage.UploadsRootPath),
        RequestPath = "/uploads"
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadStorage.ProfilePhotosRootPath),
        RequestPath = "/images/users/uploads"
    });
}

app.UseWebSockets();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapHub<CollaborationHub>("/collaborationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ResolveConfiguredPath(string? configuredPath, IWebHostEnvironment environment, string fallbackRelativePath)
{
    var path = string.IsNullOrWhiteSpace(configuredPath)
        ? Path.Combine(environment.ContentRootPath, fallbackRelativePath)
        : Environment.ExpandEnvironmentVariables(configuredPath.Trim());

    return Path.GetFullPath(Path.IsPathRooted(path)
        ? path
        : Path.Combine(environment.ContentRootPath, path));
}
