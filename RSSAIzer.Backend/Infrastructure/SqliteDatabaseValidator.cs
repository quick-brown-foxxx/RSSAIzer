using RSSAIzer.Backend.Options;

namespace RSSAIzer.Backend.Infrastructure;

/// <summary>
/// Provides fail-fast validation for SQLite database directory and file permissions.
/// </summary>
internal static class SqliteDatabaseValidator
{
    /// <summary>
    /// Validates and ensures the database directory exists with proper permissions.
    /// Throws InvalidOperationException if validation fails.
    /// </summary>
    /// <param name="options">Backend deployment options containing database configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when directory creation or permission checks fail.</exception>
    public static void ValidateDatabasePermissions(BackendDeploymentOptions options)
    {
        var dbDirectory = options.GetDatabaseDirectory();
        var dbFile = options.GetDatabaseFile();

        // we have no logger yet on this lifecycle step
        Console.WriteLine($"Database directory: {dbDirectory?.FullName ?? "current directory"}");
        Console.WriteLine($"Database file: {dbFile.FullName}");

        // Fail-fast: Ensure directory exists and has proper permissions
        if (dbDirectory is not null)
        {
            EnsureDirectoryExists(dbDirectory);
            ValidateDirectoryWritePermissions(dbDirectory);
        }
        else
        {
            // Database is in current directory - check current directory is writable
            ValidateCurrentDirectoryWritePermissions();
        }

        // Fail-fast: Check if database file exists but is not writable
        if (dbFile.Exists)
        {
            ValidateFileWritePermissions(dbFile);
        }
    }

    private static void EnsureDirectoryExists(DirectoryInfo directory)
    {
        if (!directory.Exists)
        {
            try
            {
                directory.Create();
                Console.WriteLine($"Created database directory: {directory.FullName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create database directory '{directory.FullName}'. "
                        + $"Check permissions and ensure the parent directory exists. Error: {ex.Message}",
                    ex
                );
            }
        }
    }

    private static void ValidateDirectoryWritePermissions(DirectoryInfo directory)
    {
        try
        {
            var testFile = Path.Combine(directory.FullName, ".write-test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Database directory '{directory.FullName}' is not writable. "
                    + $"Check permissions. Error: {ex.Message}",
                ex
            );
        }
    }

    private static void ValidateCurrentDirectoryWritePermissions()
    {
        try
        {
            var testFile = Path.Combine(Directory.GetCurrentDirectory(), ".write-test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Current directory '{Directory.GetCurrentDirectory()}' is not writable. "
                    + $"Cannot create database file. Error: {ex.Message}",
                ex
            );
        }
    }

    private static void ValidateFileWritePermissions(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Database file '{file.FullName}' exists but is not writable. "
                    + $"Check file permissions. Error: {ex.Message}",
                ex
            );
        }
    }
}
