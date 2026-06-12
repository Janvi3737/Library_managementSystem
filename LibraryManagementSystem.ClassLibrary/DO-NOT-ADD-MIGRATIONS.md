# DO NOT use EF Core migrations on this project

The project intentionally manages its database schema **without** EF Core
migrations. All schema changes are applied at app startup, automatically.

## Why

EF migrations are provider-specific — scaffolded SQL Server migrations emit
`nvarchar(450)` etc., while scaffolded SQLite migrations emit `TEXT`.
Running one set of migrations against the other provider fails. This is
the root cause of the recurring error:

```
There is already an object named 'AspNetRoles' in the database.
```

That error happens because `EnsureCreatedAsync()` already built the
tables on first `dotnet run`, and then `Update-Database` tries to
`CREATE TABLE AspNetRoles` again on top.

## How schema is actually kept in sync

In `LibraryManagementSystem/Program.cs` (admin) AND
`Library_Management_System/Program.cs` (user), startup runs:

```csharp
await db.Database.EnsureCreatedAsync();    // first run -> create all tables
await DbSchemaPatcher.PatchAsync(db);       // add any new tables / columns
await DbSeeder.SeedAsync(db);               // seed defaults if empty
```

`DbSchemaPatcher` (`Data/DbSchemaPatcher.cs`) walks every entity, queries
`sys.columns` (SQL Server) or `PRAGMA table_info` (SQLite), and issues
`ALTER TABLE ADD COLUMN` for any property that doesn't exist yet.

## Rule

- **Never** run `Add-Migration` in Package Manager Console
- **Never** run `Update-Database` in Package Manager Console
- **Never** run `dotnet ef migrations add` from CLI
- **Never** run `dotnet ef database update` from CLI
- If a `Migrations/` folder shows up under `LibraryManagementSystem.ClassLibrary/`,
  delete it before committing

## Adding a column or table

1. Add property/class to a model under `Models/`
2. If it's a new entity, add a `DbSet<>` to `Data/AppDbContext.cs`
3. Run `dotnet run` from either app — schema patches automatically
4. Console will print `[DbSchemaPatcher] +TableName.ColumnName` confirmation

## Recovery if you already ran Update-Database

If you got `There is already an object named 'AspNetRoles' in the database`:

1. Stop. Don't run `Update-Database` again.
2. Delete the `Migrations/` folder.
3. Drop the broken DB so EnsureCreated can rebuild from scratch:
   - **SQL Server (LocalDB) — Windows**: open SSMS or VS SQL Server Object
     Explorer, right-click `LibraryManagementDB`, choose **Delete**.
     Or in a sqlcmd window:
     ```
     sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE LibraryManagementDB;"
     ```
   - **SQLite — Mac**: `rm <repo-root>/LibraryManagementDB.db`
4. Run `dotnet run` from `LibraryManagementSystem/` (admin) or
   `Library_Management_System/` (user). `EnsureCreated` rebuilds the
   schema; `DbSeeder` re-seeds the default categories, plans, etc.
5. Re-register your admin user via `/Account/Register`.

## Keeping data while clearing the broken migration

If you don't want to drop the whole DB, just remove the migration history
table so EF stops re-trying:

```sql
DROP TABLE __EFMigrationsHistory;
```

Then `dotnet run` — `DbSchemaPatcher` reconciles any missing columns
without dropping data.
