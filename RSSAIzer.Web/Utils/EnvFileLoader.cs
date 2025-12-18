using DotNetEnv;

namespace RSSAIzer.Web.Utils;

public static class EnvFileLoader
{
    /// <summary>
    /// Loads environment variables from .env file at solution root, with fallback to .env.example.
    /// </summary>
    public static void LoadEnvFile()
    {
        var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
        var envFile =
            solutionRoot != null && File.Exists(Path.Combine(solutionRoot, ".env"))
                ? Path.Combine(solutionRoot, ".env")
            : solutionRoot != null && File.Exists(Path.Combine(solutionRoot, ".env.example"))
                ? Path.Combine(solutionRoot, ".env.example")
            : null;

        if (envFile != null)
        {
            Env.Load(envFile);
        }
    }

    /// <summary>
    /// Finds the solution root directory by traversing up from the start path until a .sln file is found.
    /// </summary>
    private static string? FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0)
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }
}
