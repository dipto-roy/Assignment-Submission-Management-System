using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Application.Options;

/// <summary>
/// File upload limits and the choice of backing provider. Configured under <c>Storage</c>.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public const string ProviderAuto = "Auto";
    public const string ProviderCloudinary = "Cloudinary";
    public const string ProviderLocal = "Local";

    /// <summary>
    /// <c>Cloudinary</c>, <c>Local</c>, or <c>Auto</c> (the default): use Cloudinary when
    /// credentials are configured and fall back to local disk otherwise. Auto exists so a
    /// fresh <c>docker compose up</c> still works for someone without a Cloudinary account;
    /// the resolved choice is logged at startup rather than left implicit.
    /// </summary>
    public string Provider { get; set; } = ProviderAuto;

    /// <summary>Per-file ceiling. Enforced before a single byte reaches the provider.</summary>
    [Range(1, 100 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Cap per assignment or submission, so one user cannot fill the store.</summary>
    [Range(1, 50)]
    public int MaxFilesPerOwner { get; set; } = 5;

    /// <summary>Root directory for the local provider. Mounted as a volume in docker-compose.</summary>
    public string LocalRootPath { get; set; } = "/app/uploads";

    public CloudinaryOptions Cloudinary { get; set; } = new();

    public bool HasCloudinaryCredentials =>
        !string.IsNullOrWhiteSpace(Cloudinary.CloudName)
        && !string.IsNullOrWhiteSpace(Cloudinary.ApiKey)
        && !string.IsNullOrWhiteSpace(Cloudinary.ApiSecret);

    /// <summary>
    /// The provider actually in force once <c>Auto</c> is resolved. Throws when Cloudinary is
    /// demanded explicitly but not configured, rather than silently degrading to local disk —
    /// a deployment that asked for Cloudinary and got a container-local directory would lose
    /// every uploaded file on the next container recreate.
    /// </summary>
    public string ResolveProvider() => Provider switch
    {
        ProviderCloudinary when !HasCloudinaryCredentials => throw new InvalidOperationException(
            "Storage:Provider is set to Cloudinary but Storage:Cloudinary:CloudName, :ApiKey and "
            + ":ApiSecret are not all configured. Set them (see .env.example) or use Storage__Provider=Local."),
        ProviderCloudinary => ProviderCloudinary,
        ProviderLocal => ProviderLocal,
        ProviderAuto => HasCloudinaryCredentials ? ProviderCloudinary : ProviderLocal,
        _ => throw new InvalidOperationException(
            $"Storage:Provider must be one of {ProviderAuto}, {ProviderCloudinary} or {ProviderLocal}; got '{Provider}'.")
    };
}

public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Folder prefix applied to every public id, keeping this app's files together.</summary>
    public string Folder { get; set; } = "assignment-submission";
}
