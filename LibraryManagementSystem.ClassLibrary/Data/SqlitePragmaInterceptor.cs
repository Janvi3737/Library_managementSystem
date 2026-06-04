using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Runs the concurrency PRAGMAs on every SqliteConnection EF opens —
    /// not just the bootstrap one. Without this, EF's connection pool can
    /// hand out connections that never had busy_timeout set, and any
    /// SaveChanges that runs while the other app holds a lock throws
    /// SQLITE_BUSY immediately instead of waiting.
    ///
    /// journal_mode is persisted in the .db file, so running it again is
    /// a cheap no-op. busy_timeout is per-connection and MUST be set on
    /// each one.
    ///
    /// Registered via .AddInterceptors(new SqlitePragmaInterceptor()) on
    /// the Sqlite branch of AddDbContext in both apps' Program.cs.
    /// </summary>
    public class SqlitePragmaInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            ApplyPragmas(connection);
        }

        public override Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            ApplyPragmas(connection);
            return Task.CompletedTask;
        }

        private static void ApplyPragmas(DbConnection connection)
        {
            // This interceptor is wired up ONLY on the Sqlite branch in
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "PRAGMA journal_mode = WAL;" +
                "PRAGMA busy_timeout = 5000;" +
                // foreign_keys is OFF by default in Sqlite (per-connection, not
                "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }
    }
}
