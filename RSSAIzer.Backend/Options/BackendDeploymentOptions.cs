using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using RuntimeNullables;

namespace RSSAIzer.Backend.Options;

[NullChecks(false)]
internal record BackendDeploymentOptions : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Max concurrent AI tasks must be a positive integer")]
    [Required(ErrorMessage = "MAX_CONCURRENT_AI_TASKS configuration option was not set")]
    [ConfigurationKeyName("MAX_CONCURRENT_AI_TASKS")]
    public required int MaxConcurrentAiTasks { get; init; }

    [Required(ErrorMessage = "DATABASE_DIRECTORY configuration option was not set")]
    [ConfigurationKeyName("DATABASE_DIRECTORY")]
    public required string DatabaseDirectory { get; init; }

    [Required(ErrorMessage = "DATABASE_FILE_NAME configuration option was not set")]
    [ConfigurationKeyName("DATABASE_FILE_NAME")]
    public required string DatabaseFileName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(DatabaseDirectory))
        {
            yield return new ValidationResult(
                "DATABASE_DIRECTORY cannot be empty.",
                new[] { nameof(DatabaseDirectory) }
            );
        }

        if (string.IsNullOrWhiteSpace(DatabaseFileName))
        {
            yield return new ValidationResult(
                "DATABASE_FILE_NAME cannot be empty.",
                new[] { nameof(DatabaseFileName) }
            );
        }

        // Validate that database file name is a valid filename
        if (!string.IsNullOrWhiteSpace(DatabaseFileName))
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            if (DatabaseFileName.IndexOfAny(invalidChars) >= 0)
            {
                var foundInvalidChars = DatabaseFileName
                    .Where(c => invalidChars.Contains(c))
                    .Distinct()
                    .ToArray();
                yield return new ValidationResult(
                    $"DATABASE_FILE_NAME contains invalid characters: {string.Join(", ", foundInvalidChars)}",
                    new[] { nameof(DatabaseFileName) }
                );
            }
        }
    }
}

internal static class BackendDeploymentOptionsExtensions
{
    /// <summary>
    /// Builds a SQLite connection string using SqliteConnectionStringBuilder.
    /// Paths are normalized to absolute paths to handle relative paths correctly across platforms.
    /// </summary>
    public static string GetConnectionString(this BackendDeploymentOptions options)
    {
        // Combine directory and filename, then normalize to absolute path
        var dbPath = Path.Combine(options.DatabaseDirectory, options.DatabaseFileName);
        var normalizedPath = Path.GetFullPath(dbPath);

        // Use SqliteConnectionStringBuilder for structured connection string building
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = normalizedPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Gets the database file info with normalized absolute path.
    /// </summary>
    public static FileInfo GetDatabaseFile(this BackendDeploymentOptions options)
    {
        var dbPath = Path.Combine(options.DatabaseDirectory, options.DatabaseFileName);
        var normalizedPath = Path.GetFullPath(dbPath);
        return new FileInfo(normalizedPath);
    }

    /// <summary>
    /// Gets the database directory info with normalized absolute path.
    /// Returns null if the database file is in the current directory (no directory component).
    /// </summary>
    public static DirectoryInfo? GetDatabaseDirectory(this BackendDeploymentOptions options)
    {
        var dbFile = options.GetDatabaseFile();
        return dbFile.DirectoryName is { } directoryName ? new DirectoryInfo(directoryName) : null;
    }
}
