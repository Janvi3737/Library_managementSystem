using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Design-time factory used by `dotnet ef` tooling (migrations add,
    /// migrations list, etc.) when the tools can't construct an AppDbContext
    /// from a running web host — typically when invoked from the class library
    /// project directly.
    ///
    /// Platform-aware:
    ///   - Windows  → SqlServer LocalDB (matches the runtime default on Windows)
    ///   - Mac/Linux → Sqlite at the solution root (matches the runtime default
    ///                 on those platforms; the same shared .db both apps use)
    ///
    /// IMPORTANT — runtime vs design-time:
    ///   At runtime, schema is created from the EF model via EnsureCreatedAsync
    ///   on Sqlite and via MigrateAsync on SqlServer. So:
    ///     - On Mac you NEVER need to run `dotnet ef database update`.
    ///       Just `dotnet run`. The app builds the schema on first start.
    ///     - On Windows you also don't usually need to run it manually —
    ///       startup MigrateAsync applies any new migration files automatically.
    ///
    ///   Running `dotnet ef database update` on Mac is now harmless with this
    ///   factory (no more "LocalDB is not supported" crash) but the SqlServer-
    ///   flavored migration files won't translate cleanly to Sqlite. Stick to
    ///   `dotnet run`.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();

            if (OperatingSystem.IsWindows())
            {
                const string sqlServerConn =
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=LibraryManagementDB;" +
                    "Trusted_Connection=True;" +
                    "MultipleActiveResultSets=true;" +
                    "TrustServerCertificate=True";

                builder.UseSqlServer(sqlServerConn);
            }
            else
            {
                // Resolve to the solution-root LibraryManagementDB.db.
                var solutionRoot = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
                var sqliteFile = Path.Combine(solutionRoot, "LibraryManagementDB.db");

                builder.UseSqlite($"Data Source={sqliteFile}");
            }

            return new AppDbContext(builder.Options);
        }
    }
}
