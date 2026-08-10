namespace AssignmentSubmissionSystem.Api.Configuration;

/// <summary>
/// Registers the local <c>.env</c> file as a configuration source.
/// </summary>
public static class DotEnvConfigurationExtensions
{
    /// <summary>
    /// Adds <c>.env</c> (searched upward from the content root) as a configuration source,
    /// then re-applies environment variables so that real environment variables — the ones
    /// docker-compose and CI inject — always win over the file.
    /// </summary>
    /// <remarks>
    /// No-op when the file is absent, which is the expected case in Docker and CI.
    /// </remarks>
    public static void AddDotEnvFile(this IConfigurationBuilder configuration, string contentRootPath)
    {
        var path = DotEnvFile.Find(contentRootPath);
        if (path is null)
        {
            return;
        }

        IReadOnlyDictionary<string, string?> values;
        try
        {
            values = DotEnvFile.Parse(File.ReadLines(path));
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to read the environment file at '{path}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Not permitted to read the environment file at '{path}'.", ex);
        }

        configuration.AddInMemoryCollection(values);
        configuration.AddEnvironmentVariables();
    }
}
