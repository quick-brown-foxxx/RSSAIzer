using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RSSAIzer.Web.Utils;

public static class ViteManifestHelper
{
    [UsedImplicitly]
    private sealed record ManifestEntry
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("css")]
        public List<string>? Css { get; set; }

        [JsonPropertyName("assets")]
        public List<string>? Assets { get; set; }
    }

    public static IHtmlContent LoadViteAssets(
        this IHtmlHelper _,
        string assetName,
        IWebHostEnvironment env
    )
    {
        var manifestPath = Path.Combine(env.WebRootPath, "build", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new UnreachableException("Vite assets misconfigured! No manifest present.");
        }

        var manifest = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(
            File.ReadAllText(manifestPath)
        );
        if (manifest == null)
        {
            throw new UnreachableException(
                "Vite assets misconfigured! Failed to deserialize manifest."
            );
        }
        if (!manifest.TryGetValue(assetName, out var entry))
        {
            throw new UnreachableException(
                $"Vite assets misconfigured! Failed to load asset {assetName}"
            );
        }
        if (entry.Assets == null && entry.Css == null && entry.File == null)
        {
            throw new UnreachableException(
                $"Vite assets misconfigured! All properties of asset are null: {assetName}"
            );
        }

        var tags = new List<string>();

        // Add CSS referenced by JS entry (e.g., bootstrap, icons)
        if (entry.Css != null)
        {
            tags.AddRange(
                entry.Css.Select(cssFile =>
                    $"<link rel=\"stylesheet\" href=\"/build/{cssFile}\" />"
                )
            );
        }
        // Add the main asset (js or css)
        if (entry.File != null && entry.File.EndsWith(".css"))
        {
            tags.Add($"<link rel=\"stylesheet\" href=\"/build/{entry.File}\" />");
        }
        else if (entry.File != null && entry.File.EndsWith(".js"))
        {
            tags.Add(
                $"<script type=\"application/javascript\" src=\"/build/{entry.File}\"></script>"
            );
        }

        return new HtmlString(string.Join("\n", tags));
    }
}
