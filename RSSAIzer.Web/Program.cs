using System.Diagnostics;
using Microsoft.Extensions.Options;
using RSSAIzer.Backend;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Web.Infrastructure.Auth;
using RSSAIzer.Web.Options;
using RSSAIzer.Web.Services;
using RSSAIzer.Web.Utils;

var builder = WebApplication.CreateBuilder(args);

EnvFileLoader.LoadEnvFile();
builder.Configuration.AddEnvironmentVariables();
builder
    .Services.AddOptions<WebDeploymentOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder
    .Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Explicitly validate options immediately
#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
using (var tempSp = builder.Services.BuildServiceProvider())
{
    _ = tempSp.GetRequiredService<IOptions<AuthOptions>>().Value;
    _ = tempSp.GetRequiredService<IOptions<WebDeploymentOptions>>().Value;
    tempSp.Dispose();
}
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

// Backend configuration
builder.AddBackendCustom();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackendClient>();
builder.Services.AddSession();

builder.Services.AddAuthenticationCustom();

// Pass authentication configuration to the backend
builder.Services.AddTransient(provider =>
{
    var authOptions = provider.GetRequiredService<IOptions<AuthOptions>>().Value;
    return new BackendAuthConfiguration(
        authOptions.ReverseProxyHeaderId,
        authOptions.GetMode() switch
        {
            AuthMode.SingleUser => AuthenticationMode.SingleUser,
            AuthMode.ReverseProxy => AuthenticationMode.ReverseProxy,
            AuthMode.OpenIdConnect => AuthenticationMode.OpenIdConnect,
            _ => throw new UnreachableException($"Unknown auth mode: {authOptions.GetMode()}"),
        },
        authOptions.SingleUserId,
        authOptions.SingleUserEmail
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var deploymentOptions = app.Services.GetRequiredService<IOptions<WebDeploymentOptions>>().Value;

app.UsePathBase(deploymentOptions.BasePath);
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

await app.UseBackendCustom();

app.Run();
