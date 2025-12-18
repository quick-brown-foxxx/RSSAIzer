using RSSAIzer.Backend.Db;

namespace RSSAIzer.Backend.Infrastructure;

/// <summary>
/// Ensures that a user exists in the database; creates user if missing.
/// </summary>
internal interface IUserPersistenceService
{
    /// <summary>
    /// Checks if user exists, creates if not. Idempotent.
    /// </summary>
    Task EnsureUserExistsAsync(Guid userId, string email, CancellationToken ct);
}

internal sealed class UserPersistenceService(ApplicationDbContext context) : IUserPersistenceService
{
    public async Task EnsureUserExistsAsync(Guid userId, string email, CancellationToken ct)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
        {
            return;
        }

        context.Users.Add(
            new()
            {
                Id = userId,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
            }
        );
        await context.SaveChangesAsync(ct);
    }
}
