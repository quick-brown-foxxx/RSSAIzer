namespace RSSAIzer.Web.Utils;

/// <summary>
/// Centralized route constants for Razor Pages and endpoints to avoid magic strings.
/// </summary>
public static class Routes
{
    // Root pages
    public const string Page_Index = "/Index";

    // Digest pages (singular)
    public const string Page_DigestIndex = "/Digest/Index";
    public const string Page_DigestProgress = "/Digest/Progress";

    // Digests pages (plural)
    public const string Page_DigestsIndex = "/Digests/Index";
    public const string Page_DigestsGenerate = "/Digests/Generate";
    public const string Page_DigestsQueue = "/Digests/Queue";

    // Feeds pages
    public const string Page_FeedsIndex = "/Feeds/Index";
    public const string Page_FeedsAdd = "/Feeds/Add";

    // Settings pages
    public const string Page_SettingsIndex = "/Settings/Index";

    // Endpoints (controllers)
    public const string Endpoint_AuthLogin = "/Auth/Login";
    public const string Endpoint_AuthLogout = "/Auth/Logout";
}
