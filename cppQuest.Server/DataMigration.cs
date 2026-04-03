using Microsoft.Data.Sqlite;

namespace cppQuest.Server;

/// <summary>
/// Переносит данные из cppquest.db.backup (старая схема) в новую БД.
/// Запускается один раз при старте, если Users пустая.
/// </summary>
public static class DataMigration
{
    /// <summary>
    /// Применяет патчи схемы (ALTER TABLE), которые не покрыты EF-миграциями.
    /// Безопасно запускать при каждом старте — проверяет наличие колонки.
    /// </summary>
    public static async Task ApplySchemaPatches(string connectionString, ILogger logger)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        // Patch 1: TestTitle в ParagraphTestStats
        var hasTestTitle = Convert.ToInt64(await ScalarAsync(conn,
            "SELECT COUNT(*) FROM pragma_table_info('ParagraphTestStats') WHERE name='TestTitle'"));
        if (hasTestTitle == 0)
        {
            await ExecRawAsync(conn,
                "ALTER TABLE ParagraphTestStats ADD COLUMN TestTitle TEXT NOT NULL DEFAULT ''");
            logger.LogInformation("SchemaPatches: Added TestTitle column to ParagraphTestStats.");
        }

        // Patch 2: Freeze/Snowflake columns in GamificationProfiles
        var hasFreezeCount = Convert.ToInt64(await ScalarAsync(conn,
            "SELECT COUNT(*) FROM pragma_table_info('GamificationProfiles') WHERE name='FreezeCount'"));
        if (hasFreezeCount == 0)
        {
            await ExecRawAsync(conn,
                "ALTER TABLE GamificationProfiles ADD COLUMN FreezeCount INTEGER NOT NULL DEFAULT 0");
            await ExecRawAsync(conn,
                "ALTER TABLE GamificationProfiles ADD COLUMN SnowflakeCount INTEGER NOT NULL DEFAULT 0");
            await ExecRawAsync(conn,
                "ALTER TABLE GamificationProfiles ADD COLUMN TotalStreakDays INTEGER NOT NULL DEFAULT 0");
            logger.LogInformation("SchemaPatches: Added FreezeCount/SnowflakeCount/TotalStreakDays to GamificationProfiles.");
        }
        // Patch 3: UnlockedContentStds in GamificationProfiles
        var hasUnlockedStds = Convert.ToInt64(await ScalarAsync(conn,
            "SELECT COUNT(*) FROM pragma_table_info('GamificationProfiles') WHERE name='UnlockedContentStds'"));
        if (hasUnlockedStds == 0)
        {
            await ExecRawAsync(conn,
                "ALTER TABLE GamificationProfiles ADD COLUMN UnlockedContentStds TEXT NOT NULL DEFAULT ''");
            logger.LogInformation("SchemaPatches: Added UnlockedContentStds to GamificationProfiles.");
        }

        // Patch 4: UnlockedSlots in GamificationProfiles
        var hasUnlockedSlots = Convert.ToInt64(await ScalarAsync(conn,
            "SELECT COUNT(*) FROM pragma_table_info('GamificationProfiles') WHERE name='UnlockedSlots'"));
        if (hasUnlockedSlots == 0)
        {
            await ExecRawAsync(conn,
                "ALTER TABLE GamificationProfiles ADD COLUMN UnlockedSlots TEXT NOT NULL DEFAULT ''");
            logger.LogInformation("SchemaPatches: Added UnlockedSlots to GamificationProfiles.");
        }
    }

    public static async Task RunAsync(string connectionString, ILogger logger)
    {
        // Вычисляем путь к бэкапу рядом с основной БД
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dbPath = builder.DataSource;
        var backupPath = dbPath + ".backup";

        if (!File.Exists(backupPath))
        {
            logger.LogInformation("DataMigration: No backup file found at {Path}, skipping.", backupPath);
            return;
        }

        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        // Если Users уже заполнена — миграция уже была
        var usersCount = Convert.ToInt64(await ScalarAsync(conn, "SELECT COUNT(*) FROM Users"));
        if (usersCount > 0)
        {
            logger.LogInformation("DataMigration: Users already has {Count} rows, skipping.", usersCount);
            return;
        }

        logger.LogInformation("DataMigration: Attaching backup and migrating Students → Users + GamificationProfiles...");

        // Подключаем бэкап как отдельную БД
        var escapedBackupPath = backupPath.Replace("'", "''");
        await ExecRawAsync(conn, $"ATTACH DATABASE '{escapedBackupPath}' AS backup");

        // Проверяем что в бэкапе есть таблица Students
        var hasStudents = Convert.ToInt64(await ScalarAsync(conn,
            "SELECT COUNT(*) FROM backup.sqlite_master WHERE type='table' AND name='Students'"));

        if (hasStudents == 0)
        {
            logger.LogInformation("DataMigration: No Students table in backup, skipping.");
            await ExecRawAsync(conn, "DETACH DATABASE backup");
            return;
        }

        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();
        try
        {
            await ExecAsync(conn, tx, @"
                INSERT INTO Users (Id, FirstName, LastName, IsuNumber, PasswordHash, IsActive, RegisteredAt, LastLoginDate)
                SELECT Id, FirstName, LastName, IsuNumber, PasswordHash, 1,
                       COALESCE(RegisteredAt, datetime('now')),
                       LastLoginDate
                FROM backup.Students");

            await ExecAsync(conn, tx, @"
                INSERT INTO GamificationProfiles (UserId, Xp, Level, Coins, CurrentStreak, MaxStreak, LastActivityDate)
                SELECT Id,
                       COALESCE(TotalPoints, 0),
                       (COALESCE(TotalPoints, 0) / 500) + 1,
                       0,
                       COALESCE(CurrentStreak, 0),
                       COALESCE(MaxStreak, 0),
                       COALESCE(LastLoginDate, datetime('now'))
                FROM backup.Students");

            await tx.CommitAsync();
            logger.LogInformation("DataMigration: Done.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "DataMigration: Failed, rolled back.");
            throw;
        }
        finally
        {
            await ExecRawAsync(conn, "DETACH DATABASE backup");
        }
    }

    private static async Task<object?> ScalarAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task ExecRawAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecAsync(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
