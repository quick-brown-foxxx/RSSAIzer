using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Web.Options;

[NullChecks(false)]
public sealed record WebDeploymentOptions
{
    [UrlBasePath(memberName: "BASEPATH")]
    [Required(ErrorMessage = "BASEPATH configuration option was not set")]
    [ConfigurationKeyName("BASEPATH")]
    public required string BasePath { get; set; }
}
