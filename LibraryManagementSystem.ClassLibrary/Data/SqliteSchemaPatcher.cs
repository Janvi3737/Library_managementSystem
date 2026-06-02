using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Generic Sqlite schema reconciler — keeps the on-disk schema in sync
    /// with the current EF model, no manual updates required when models change.
    ///
    /// Why this is needed:
    ///   EF Core's EnsureCreated is a one-shot operation. The moment any tables
    ///   exist in the .db file, EnsureCreated does nothing — even for tables
    ///   or columns added to the model later. Migrations would handle this on
    ///   SqlServer, but we don't run Sqlite-flavored migrations on Mac/Linux.
    ///   Result: every model edit broke an existing Mac dev DB until someone
    ///   manually dropped the table.
    ///
    /// What this does, automatically, on every startup:
    ///   1. Generates the full CREATE script EF would produce for the current
    ///      model, rewrites it with IF NOT EXISTS, runs it. Any NEW table
    ///      the model defines gets created; existing tables are left alone.
    ///   2. Walks every entity in the EF model, queries PRAGMA table_info
    ///      for the actual columns, ALTER TABLE ADD COLUMN for any new
    ///      properties the table doesn't have yet.
    ///
    /// Limitations (Sqlite ALTER TABLE constraints):
    ///   - Can only ADD nullable columns (or NOT NULL with a DEFAULT).
    ///     We skip non-nullable adds — those need a manual migration.
    ///   - Cannot drop columns. EF dropping a property from a model leaves
    ///     a stale column in the .db. Harmless.
    ///   - Cannot retroactively add FK / UNIQUE constraints to existing
    ///     columns. The CREATE TABLE in step 1 puts them on new tables;
    ///     existing tables stay as-is.
    ///
    /// All operations are idempotent — safe to run on every app start.
    /// </summary>
    public static class SqliteSchemaPatcher
    {
        public static async Task PatchAsync(AppDbContext db)
        {
            if (!db.Database.IsSqlite())
                return;

            await CreateMissingTablesAsync(db);
            await AddMissingColumnsAsync(db);
        }

        // ───── 1. CREATE TABLE / INDEX for anything the model has but the DB doesn't ─────

        private static async Task CreateMissingTablesAsync(AppDbContext db)
        {
            // EF generates the full DDL for the CURRENT model. We rewrite
            // CREATE TABLE / CREATE INDEX to be idempotent so re-running on
            // an existing DB is safe — only missing objects get created.
            var script = db.Database.GenerateCreateScript();

            script = Regex.Replace(
                script,
                @"\bCREATE TABLE\s+",
                "CREATE TABLE IF NOT EXISTS ",
                RegexOptions.IgnoreCase);

            script = Regex.Replace(
                script,
                @"\bCREATE (UNIQUE )?INDEX\s+",
                m => $"CREATE {m.Groups[1].Value}INDEX IF NOT EXISTS ",
                RegexOptions.IgnoreCase);

            try
            {
                await db.Database.ExecuteSqlRawAsync(script);
            }
            catch (Exception ex)
            {
                // The script may contain a handful of statements that aren't
                // strictly idempotent (e.g., a CHECK constraint that conflicts
                // with rewritten table). Don't crash the app — log and move on
                // to the column patcher which handles the common drift case.
                Console.WriteLine(
                    $"[SqliteSchemaPatcher] CREATE TABLE/INDEX pass: {ex.Message}");
            }
        }

        // ───── 2. ALTER TABLE ADD COLUMN for any new properties on existing tables ─────

        private static async Task AddMissingColumnsAsync(AppDbContext db)
        {
            foreach (var entityType in db.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName))
                    continue;

                // Skip entities mapped to views / queries / owned types
                if (entityType.IsOwned())
                    continue;

                var existingCols = await GetSqliteColumnsAsync(db, tableName);
                if (existingCols.Count == 0)
                    continue; // table didn't exist; CREATE TABLE pass above handled it

                foreach (var prop in entityType.GetProperties())
                {
                    var colName = prop.GetColumnName();
                    if (string.IsNullOrEmpty(colName))
                        continue;
                    if (existingCols.Contains(colName))
                        continue; // already there

                    var colType = ResolveSqliteType(prop);

                    // Sqlite ALTER TABLE ADD COLUMN can't add a NOT NULL column
                    // without a DEFAULT. For nullable model properties just add
                    // NULL; for non-nullable, derive a sensible zero-value default
                    // from the CLR type so existing rows have a valid value.
                    string nullability;
                    if (prop.IsNullable)
                    {
                        nullability = "NULL";
                    }
                    else
                    {
                        var defaultLiteral = ResolveSqliteDefault(prop);
                        nullability = $"NOT NULL DEFAULT {defaultLiteral}";
                    }

                    try
                    {
                        await db.Database.ExecuteSqlRawAsync(
                            $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{colName}\" {colType} {nullability};");

                        Console.WriteLine(
                            $"[SqliteSchemaPatcher] Added missing column {tableName}.{colName} ({colType} {nullability})");
                    }
                    catch (Exception ex)
                    {
                        // Most common cause: race with a parallel patch run, or
                        // the column was added between our check and the ALTER.
                        Console.WriteLine(
                            $"[SqliteSchemaPatcher] Skipped {tableName}.{colName}: {ex.Message}");
                    }
                }
            }
        }

        // ───── helpers ─────

        private static async Task<HashSet<string>> GetSqliteColumnsAsync(
            AppDbContext db, string tableName)
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var conn = db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
                await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // PRAGMA table_info columns: cid, name, type, notnull, dflt_value, pk
                    cols.Add(reader.GetString(1));
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
            // Prefer the column type EF would emit. Falls back to a CLR-type
            // mapping for Sqlite's small set of storage classes.
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

            // DateTime, DateTimeOffset, TimeSpan, Guid, string, enum → TEXT
            return "TEXT";
        }

        private static string ResolveSqliteDefault(IProperty prop)
        {
            // For NOT NULL columns added retroactively, pick a "zero value"
            // appropriate to the storage class so existing rows stay valid.
            var t = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;

            if (t == typeof(bool) ||
                t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                t == typeof(byte))
                return "0";

            if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                return "0";

            if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
                return "'1970-01-01 00:00:00'";

            if (t == typeof(Guid))
                return "'00000000-0000-0000-0000-000000000000'";

            if (t.IsEnum)
                return "0";

            // string / TimeSpan / fallback
            return "''";
        }
    }
}
