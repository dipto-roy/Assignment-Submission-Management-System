namespace AssignmentSubmissionSystem.Api.Configuration;

/// <summary>
/// Minimal <c>.env</c> reader for local development.
/// </summary>
/// <remarks>
/// docker-compose reads <c>.env</c> natively, but <c>dotnet run</c> does not. Without this,
/// the values in <c>.env</c> are silently ignored outside Docker and <c>appsettings.Development.json</c>
/// wins instead. This closes that gap so both run modes read the same file.
/// <para>
/// Keys use the environment-variable convention (<c>Jwt__Key</c>), which is translated to the
/// configuration convention (<c>Jwt:Key</c>). Real environment variables take precedence over
/// the file — see <see cref="DotEnvConfigurationExtensions.AddDotEnvFile"/>.
/// </para>
/// </remarks>
public static class DotEnvFile
{
    private const string FileName = ".env";
    private const int MaxParentDirectoriesToSearch = 6;

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for a <c>.env</c> file.
    /// Needed because the content root differs between <c>dotnet run</c> (src/Api) and the
    /// test host, while <c>.env</c> lives at the backend root.
    /// </summary>
    /// <returns>The full path to the file, or <c>null</c> when no file is found.</returns>
    public static string? Find(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var depth = 0; depth <= MaxParentDirectoriesToSearch && directory is not null; depth++)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Parses a <c>.env</c> file into configuration keys.
    /// </summary>
    /// <remarks>
    /// Supports comments (<c>#</c>), blank lines, an optional <c>export</c> prefix, and
    /// single- or double-quoted values. Values are split on the <em>first</em> <c>=</c> only, so
    /// connection strings and base64 secrets (which may end in <c>=</c>) survive intact.
    /// Inline comments are deliberately <em>not</em> stripped from unquoted values — a <c>#</c>
    /// inside a generated secret is far more likely than a trailing comment.
    /// </remarks>
    public static IReadOnlyDictionary<string, string?> Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                // No key, or no '=' at all. Skip rather than throw: a malformed comment
                // should not stop the application from starting.
                continue;
            }

            var key = line[..separatorIndex].TrimEnd();
            var value = Unquote(line[(separatorIndex + 1)..].Trim());

            // '__' is the environment-variable spelling of the configuration ':' separator.
            values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
        }

        return values;
    }

    private static string Unquote(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var isQuoted = (value[0] == '"' && value[^1] == '"')
            || (value[0] == '\'' && value[^1] == '\'');

        return isQuoted ? value[1..^1] : value;
    }
}
