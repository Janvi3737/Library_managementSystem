using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Cross-provider schema reconciler — keeps the on-disk schema in sync
    /// with the current EF model, no manual `dotnet ef database update`
    /// required when models change. Works for both Sqlite (Mac/Linux dev)
    /// and SQL Server LocalDB (Windows dev).
    ///
    /// Why this exists:
    ///   - EF Core's EnsureCreated is a one-shot operation — once any tables
    ///     exist, it does nothing, even for new tables/columns added later.
    ///   - EF migrations are provider-specific. A migration scaffolded on
    ///     Sqlite uses TEXT/INTEGER literals that fail on SQL Server (and
    ///     vice versa). Mixing providers + one set of migrations -> pain.
    ///   - Result before this patcher: every model edit broke an existing
    ///     dev DB on at least one OS until someone manually dropped tables
    ///     or wrote ALTER scripts by hand.
    ///
    /// What this does, automatically, on every startup:
    ///   1. Generates the full CREATE script EF would emit for the CURRENT
    ///      model, rewrites it as idempotent, runs it. Any NEW table is
    ///      created; existing tables are left alone.
    ///   2. Walks every entity in the EF model, queries the provider's
    ///      catalog for the actual columns, ALTER TABLE ADD COLUMN for any
    ///      property the table doesn't have yet.
    ///
    /// Limitations:
    ///   - Cannot drop columns. EF dropping a property from the model leaves
    ///     a stale column in the DB. Harmless.
    ///   - Cannot retroactively add FK / UNIQUE constraints to existing
    ///     columns. The CREATE TABLE pass puts them on new tables; existing
    ///     tables keep whatever constraints they had.
    ///
    /// All operations are idempotent — safe to run on every app start.
    /// </summary>
    public static class DbSchemaPatcher
    {
        public static async Task PatchAsync(AppDbContext db)
        {
            if (!db.Database.IsSqlite() && !db.Database.IsSqlServer())
                return;

            await CreateMissingTablesAsync(db);
            await AddMissingColumnsAsync(db);
        }

        // ───── 1. CREATE TABLE / INDEX pass ─────

        private static async Task CreateMissingTablesAsync(AppDbContext db)
        {
            var script = db.Database.GenerateCreateScript();

            if (db.Database.IsSqlite())
            {
                // Sqlite supports IF NOT EXISTS directly on CREATE TABLE / INDEX.
                script = Regex.Replace(script,
                    @"\bCREATE TABLE\s+",
                    "CREATE TABLE IF NOT EXISTS ",
                    RegexOptions.IgnoreCase);

                script = Regex.Replace(script,
                    @"\bCREATE (UNIQUE )?INDEX\s+",
                    m => $"CREATE {m.Groups[1].Value}INDEX IF NOT EXISTS ",
                    RegexOptions.IgnoreCase);

                try { await db.Database.ExecuteSqlRawAsync(script); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbSchemaPatcher/Sqlite] CREATE pass: {ex.Message}");
                }
            }
            else
            {
                // SQL Server has no IF NOT EXISTS on CREATE TABLE — execute
                foreach (var stmt in SplitSqlServerBatches(script))
                {
                    var trimmed = stmt.Trim();
                    if (trimmed.Length == 0) continue;
                    try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                    catch (Exception ex)
                    {
                        // Most common: "There is already an object named X" — fine.
                        Console.WriteLine($"[DbSchemaPatcher/SqlServer] skip: {ex.Message.Split('\n')[0]}");
                    }
                }
            }
        }

        // EF's GenerateCreateScript for SQL Server uses GO separators only
        private static IEnumerable<string> SplitSqlServerBatches(string script)
        {
            return Regex.Split(script, @";\s*[\r\n]+");
        }

        // ───── 2. ALTER TABLE ADD COLUMN pass ─────

        private static async Task AddMissingColumnsAsync(AppDbContext db)
        {
            var isSqlite = db.Database.IsSqlite();

            foreach (var entityType in db.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName))
                    continue;

                if (entityType.IsOwned())
                    continue;

                var existingCols = await GetColumnsAsync(db, tableName, isSqlite);
                if (existingCols.Count == 0)
                    continue; // table didn't exist; CREATE pass handled it

                foreach (var prop in entityType.GetProperties())
                {
                    var colName = prop.GetColumnName();
                    if (string.IsNullOrEmpty(colName))
                        continue;
                    if (existingCols.Contains(colName))
                        continue;

                    var colType = isSqlite ? ResolveSqliteType(prop) : ResolveSqlServerType(prop);

                    // Both providers reject NOT NULL adds without DEFAULT on a
                    string nullability;
                    if (prop.IsNullable)
                    {
                        nullability = "NULL";
                    }
                    else
                    {
                        var defaultLiteral = ResolveDefault(prop);
                        nullability = $"NOT NULL DEFAULT {defaultLiteral}";
                    }

                    var alterSql = isSqlite
                        ? $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{colName}\" {colType} {nullability};"
                        : $"ALTER TABLE [{tableName}] ADD [{colName}] {colType} {nullability};";

                    try
                    {
                        await db.Database.ExecuteSqlRawAsync(alterSql);
                        Console.WriteLine(
                            $"[DbSchemaPatcher] +{tableName}.{colName} ({colType} {nullability})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[DbSchemaPatcher] skip {tableName}.{colName}: {ex.Message.Split('\n')[0]}");
                    }
                }
            }
        }

        // ───── helpers ─────

        private static async Task<HashSet<string>> GetColumnsAsync(
            AppDbContext db, string tableName, bool isSqlite)
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var conn = db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
                await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                if (isSqlite)
                {
                    cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        // cid, name, type, notnull, dflt_value, pk
                        cols.Add(reader.GetString(1));
                    }
                }
                else
                {
                    cmd.CommandText =
                        $"SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID('[{tableName}]');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        cols.Add(reader.GetString(0));
                    }
                }
            }
            finally
            {
                if (!wasOpen)
                    await conn.CloseAsync();
            }

            return cols;
        }

        private static string ResolveSqliteType(IProperty prop)
        {
            var efType = prop.GetColumnType();
            if (!string.IsNullOrWhiteSpace(efType))
                return efType;

            var t = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;

            if (t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                t == typeof(byte) || t == typeof(bool))
                return "INTEGER";

            if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                return "REAL";

            if (t == typeof(byte[]))
                return "BLOB";

            return "TEXT";
        }

        private static string ResolveSqlServerType(IProperty prop)
        {
            // If the EF model declares an explicit column type (e.g. via
            var efType = prop.GetColumnType();
            if (!string.IsNullOrWhiteSpace(efType) &&
                !efType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase) &&
                !efType.Equals("TEXT", StringComparison.OrdinalIgnoreCase) &&
                !efType.Equals("REAL", StringComparison.OrdinalIgnoreCase) &&
                !efType.Equals("BLOB", StringComparison.OrdinalIgnoreCase))
            {
                return efType;
            }

            var t = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;

            if (t == typeof(bool)) return "bit";
            if (t == typeof(byte)) return "tinyint";
            if (t == typeof(short)) return "smallint";
            if (t == typeof(int)) return "int";
            if (t == typeof(long)) return "bigint";
            if (t == typeof(decimal)) return "decimal(18,2)";
            if (t == typeof(double)) return "float";
            if (t == typeof(float)) return "real";
            if (t == typeof(DateTime)) return "datetime2";
            if (t == typeof(DateTimeOffset)) return "datetimeoffset";
            if (t == typeof(TimeSpan)) return "time";
            if (t == typeof(Guid)) return "uniqueidentifier";
            if (t == typeof(byte[])) return "varbinary(max)";
            if (t.IsEnum) return "int";

            // string / fallback. nvarchar(max) is the safe default.
            return "nvarchar(max)";
        }

        private static string ResolveDefault(IProperty prop)
        {
            var t = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;

            if (t == typeof(bool) ||
                t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                t == typeof(byte))
                return "0";

            if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                return "0";

            if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
                return "'1970-01-01T00:00:00'";

            if (t == typeof(Guid))
                return "'00000000-0000-0000-0000-000000000000'";

            if (t.IsEnum)
                return "0";

            // string / TimeSpan / fallback
            return "''";
        }
    }
}
