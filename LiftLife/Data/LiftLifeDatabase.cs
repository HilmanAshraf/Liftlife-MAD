using SQLite;

namespace LiftLife.Data;

/// <summary>
/// Central SQLite connection for LiftLife. Uses sqlite-net-pcl with an async
/// connection stored in the platform's app-data directory so all routines,
/// logs, equipment reports and nutrition entries persist offline.
/// </summary>
public class LiftLifeDatabase
{
    private SQLiteAsyncConnection? _connection;

    public const string DatabaseFilename = "liftlife.db3";

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    /// <summary>
    /// Lazily opens the connection and creates tables on first use.
    /// Table registration is added in Day 2 when the models are defined.
    /// </summary>
    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SQLiteAsyncConnection(DatabasePath, Flags);

        await InitializeAsync();
        return _connection;
    }

    private Task InitializeAsync()
    {
        // Tables will be registered here on Day 2 (models + CreateTableAsync calls).
        return Task.CompletedTask;
    }
}
