using Microsoft.AspNetCore.Identity;

namespace RSSAIzer.Backend.Infrastructure;

/// <summary>
/// Application user for Identity, using GUID as a primary key.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
