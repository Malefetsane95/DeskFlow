using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi("requester", options =>
    {
        options.ShouldInclude = description => description.GroupName == "requester";
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new()
            {
                Title = "DeskFlow Requester API",
                Version = "v1",
                Description = "Read-only ticket access for signed-in End Users. Sign in to DeskFlow in this browser before using Try it out; the Swagger UI sends same-origin requests using the existing Identity session cookie. This API does not use or issue API keys or bearer tokens."
            };
            return Task.CompletedTask;
        });
    });
}
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<IPrivateObjectStorage, SupabasePrivateObjectStorage>(client =>
    client.Timeout = TimeSpan.FromSeconds(45));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseConfigured = !string.IsNullOrWhiteSpace(connectionString);

if (databaseConfigured)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services
        .AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "The ConnectionStrings:DefaultConnection setting is required outside Development.");
}
var app = builder.Build();

if (databaseConfigured)
{
    await IdentityRoleSeeder.SeedAsync(app.Services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "DeskFlow Requester API";
        options.SwaggerEndpoint("/openapi/requester.json", "DeskFlow Requester API v1");
    });
}

app.UseRouting();

if (databaseConfigured)
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
