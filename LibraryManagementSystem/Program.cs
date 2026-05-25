using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.Filters;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SERVICES

builder.Services.AddControllersWithViews(opts =>
{
    // Register the audit log filter globally so every POST/PUT/DELETE by an
    // admin writes a row to AuditLogs. Read more in AuditLogFilter.cs.
    opts.Filters.Add<AuditLogFilter>();
});

builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddScoped<PdfReceiptService>();
builder.Services.AddScoped<ExportService>();

// DATABASE

var configuredProvider = builder.Configuration.GetValue<string>("Database:Provider");
var dbProvider = !string.IsNullOrWhiteSpace(configuredProvider)
    ? configuredProvider
    : (OperatingSystem.IsWindows() ? "SqlServer" : "Sqlite");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteFile = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, "..", "LibraryManagementDB.db"));

        var sqliteConn = $"Data Source={sqliteFile};Cache=Shared;Pooling=False";

        Console.WriteLine($"[Admin App] Sqlite database file: {sqliteFile}");
        Console.WriteLine($"[Admin App] Connection string:    {sqliteConn}");

        options.UseSqlite(sqliteConn);

        options.AddInterceptors(new SqlitePragmaInterceptor());
    }
    else
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.CommandTimeout(120));
    }
});

// IDENTITY

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// COOKIE SETTINGS
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".LibraryAdmin.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// ERROR HANDLING

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error500");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Home/Error404");

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// ROUTES

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


// DATABASE + ROLES

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    if (db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();

        // Concurrency fix: see SqlitePragmaInterceptor for details.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;");

        // Schema patch: EnsureCreated is one-shot — if the .db already had
        // any tables, EnsureCreated did nothing, so new tables added to the
        // model later won't appear. Patch fills in missing tables/columns.
        await SqliteSchemaPatcher.PatchAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(db);

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
{
    "Admin",
    "Member",
    "User"
};

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }
}

app.Run();
