using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.Infrastructure.Storage;

/// <summary>
/// Stores uploads in Cloudinary as <c>raw</c> assets with delivery type <c>authenticated</c>.
/// </summary>
/// <remarks>
/// Authenticated, not the default <c>upload</c> type, because an <c>upload</c> asset is served
/// to anyone who has the URL. That would put every submission outside this application's
/// authorization rules — a student's work would be readable by any party who obtained or
/// guessed the link.
/// <para>
/// Reads are proxied: <see cref="OpenReadAsync"/> signs a URL server-side and streams the
/// response back through the API, so the caller never receives a Cloudinary URL and every
/// download passes the role checks in the attachment service. Handing the signed URL to the
/// browser instead would be one request cheaper, but the URL would then be forwardable to
/// someone the rules exclude.
/// </para>
/// </remarks>
public sealed class CloudinaryFileStorage : IFileStorage
{
    public const string Provider = StorageOptions.ProviderCloudinary;

    private const string RawResourceType = "raw";
    private const string AuthenticatedType = "authenticated";

    private readonly Cloudinary cloudinary;
    private readonly StorageOptions options;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CloudinaryFileStorage> logger;

    public CloudinaryFileStorage(
        IOptions<StorageOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudinaryFileStorage> logger)
    {
        this.options = options.Value;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;

        var credentials = this.options.Cloudinary;
        cloudinary = new Cloudinary(new Account(credentials.CloudName, credentials.ApiKey, credentials.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    public string ProviderName => Provider;

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        // The public id is generated here and never derived from the client's file name: the
        // original name is untrusted and would otherwise steer where the object lands.
        var publicId = $"{options.Cloudinary.Folder}/{Guid.NewGuid():N}";

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Type = AuthenticatedType,
            UseFilename = false,
            UniqueFilename = false,
            DiscardOriginalFilename = true,
            Overwrite = false
        };

        // Resource type passed explicitly: the two-argument overload is typed for image
        // uploads, and coursework files must be stored as `raw` so Cloudinary neither
        // transcodes them nor rejects formats it does not recognise as media.
        var result = await cloudinary.UploadAsync(uploadParams, RawResourceType, cancellationToken);

        // The SDK reports failures in the payload rather than by throwing, so an unchecked
        // result would persist an attachment row pointing at bytes that were never stored.
        if (result.Error is not null)
        {
            logger.LogError(
                "Cloudinary upload failed for {FileName}: {CloudinaryError}",
                fileName,
                result.Error.Message);

            throw new InvalidOperationException($"Upload to Cloudinary failed: {result.Error.Message}");
        }

        return new StoredFile(result.PublicId, result.Bytes);
    }

    public async Task<FileContent> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var signedUrl = cloudinary.Api.Url
            .ResourceType(RawResourceType)
            .Type(AuthenticatedType)
            .Secure(true)
            .Signed(true)
            .BuildUrl(storageKey);

        var client = httpClientFactory.CreateClient(nameof(CloudinaryFileStorage));

        // ResponseHeadersRead so the body is streamed to the client as it arrives instead of
        // being buffered into memory first — a 10 MB ceiling per file times concurrent
        // downloads adds up quickly otherwise.
        var response = await client.GetAsync(signedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new InvalidOperationException(
                $"Cloudinary returned {(int)response.StatusCode} for stored file '{storageKey}'.");
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new FileContent(stream, contentType);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var result = await cloudinary.DestroyAsync(new DeletionParams(storageKey)
        {
            ResourceType = ResourceType.Raw,
            Type = AuthenticatedType
        });

        // Logged rather than thrown: the attachment row is already gone by this point, and
        // failing the request would tell the user their delete did not work when, as far as
        // the application is concerned, it did. A leaked object is a cleanup problem, not a
        // correctness one — but it must not pass unrecorded.
        if (result.Error is not null)
        {
            logger.LogWarning(
                "Cloudinary delete failed for {StorageKey}: {CloudinaryError}. The stored object may be orphaned.",
                storageKey,
                result.Error.Message);
        }
    }
}
