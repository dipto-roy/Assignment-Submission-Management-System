using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AssignmentSubmissionSystem.Api.BackgroundServices;
using AssignmentSubmissionSystem.Api.Configuration;
using AssignmentSubmissionSystem.Api.Middleware;
using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Attachments;
using AssignmentSubmissionSystem.Application.Auth;
using AssignmentSubmissionSystem.Application.Classes;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Application.Options;
using AssignmentSubmissionSystem.Application.Subjects;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Users;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;
using AssignmentSubmissionSystem.Infrastructure.Security;
using AssignmentSubmissionSystem.Infrastructure.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Local .env support ----
// docker-compose reads .env itself; this makes `dotnet run` read the same file.
// Real environment variables still take precedence over the file.
builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath);

// ---- Logging (Serilog) ----
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---- Options ----
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Bound a second time because the JWT bearer handler below needs the values during
// service registration, before the options system is available. `Get<T>()` bypasses the
// validation configured above, so the same rules are asserted explicitly here — otherwise a
// missing or short key surfaces as an opaque cryptographic error instead of a clear one.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration section is missing. Set Jwt__Key, Jwt__Issuer and Jwt__Audience "
        + "(see .env.example).");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < JwtOptions.MinimumKeyLength)
{
    throw new InvalidOperationException(
        $"Jwt:Key must be at least {JwtOptions.MinimumKeyLength} characters (256 bits) for HMAC-SHA256. "
        + "Set Jwt__Key in the environment or .env file.");
}

// ---- Database ----
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---- Health checks ----
// `/health` is anonymous and dependency-aware: it opens a connection through the DbContext,
// so compose's `depends_on: service_healthy` and an evaluator's smoke test both get a truthful
// answer instead of "the process is up but the database is not reachable".
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ---- Auth ----
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---- Auth feature (repository, password hashing, token issuance) ----
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ---- Admin domain (users, classes, subjects) ----
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();

// ---- Teacher domain (assignments) ----
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

// ---- Student domain (submissions) ----
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();

// ---- Notifications ----
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddOptions<DeadlineReminderOptions>()
    .Bind(builder.Configuration.GetSection(DeadlineReminderOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHostedService<DeadlineReminderService>();

// ---- File uploads ----
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

// The provider is resolved once, here, so the choice is visible in the startup log rather than
// being discovered when the first upload lands somewhere unexpected. `Get<T>()` is used because
// the decision is needed during registration, before the options system is available.
var storageOptions = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
    ?? new StorageOptions();
var resolvedStorageProvider = storageOptions.ResolveProvider();

if (resolvedStorageProvider == StorageOptions.ProviderCloudinary)
{
    // Named client so the Cloudinary download proxy gets pooled connections instead of a new
    // socket per request.
    builder.Services.AddHttpClient(nameof(CloudinaryFileStorage));
    builder.Services.AddScoped<IFileStorage, CloudinaryFileStorage>();
}
else
{
    builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
}

// Multipart uploads are bounded at the same ceiling the attachment rules enforce, so an
// oversized body is rejected by the server before it is buffered rather than after.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = storageOptions.MaxFileSizeBytes;
});

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// ---- Rate limiting (login brute-force guard) ----
// Only /auth/login is throttled: it is the one anonymous endpoint where repeated calls are
// worth something to an attacker. Partitioned per remote IP so one client cannot exhaust the
// budget for everyone. Limits are configurable so the integration suite can raise them.
builder.Services.AddOptions<LoginRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(LoginRateLimitOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Login, httpContext =>
    {
        // Resolved per request rather than captured here: configuration sources layered on
        // after this line (WebApplicationFactory in the integration suite does exactly that)
        // would otherwise be ignored.
        var limits = httpContext.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.PermitLimit,
                Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0
            });
    });

    // Rejections bypass the MVC pipeline, so the error envelope is written here by hand to
    // keep 429 responses shaped like every other error.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var payload = ApiResponse<object>.Fail("Too many login attempts. Please try again later.");
        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            cancellationToken);
    };
});

// ---- CORS (frontend dev origin) ----
const string frontendCorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---- MVC + Swagger ----
// Enums are exposed as names in responses (e.g. "Published"), so accept names on the way in too.
// Numeric values still deserialize, which keeps existing clients/tests working.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assignment & Submission Management System API",
        Version = "v1"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT with the 'Bearer ' prefix.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ---- Migrate on startup (safe/no-op if already applied) ----
// Demo users are seeded in Development only: their passwords are published in the README,
// so seeding outside Development would plant known admin credentials in a real database.

// Migration and seeding are serialised by an advisory lock inside MigrateAndSeedAsync, so
// concurrent start-ups (multiple replicas, or several integration-test hosts) cannot collide.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await DbSeeder.MigrateAndSeedAsync(db, passwordHasher, app.Environment.IsDevelopment());
}

// Stated explicitly at startup: "Auto" resolving to Local means uploads land inside the
// container, and an operator who expected Cloudinary needs to see that before files are lost
// on the next recreate rather than after.
app.Logger.LogInformation(
    "File storage provider: {StorageProvider} (configured as {ConfiguredProvider}).",
    resolvedStorageProvider,
    storageOptions.Provider);

// ---- Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serilog wraps the exception middleware so it logs the *final* status code
// (401/404/etc.) instead of the raw 500 it would see if an exception passed through it unhandled.
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Only redirect when an HTTPS endpoint actually exists. In the container the app listens on
// http://+:8080 only and TLS terminates upstream, where this middleware cannot resolve a
// target port and logs a warning on every request instead of redirecting.
if (builder.Configuration["ASPNETCORE_HTTPS_PORTS"] is not null
    || builder.Configuration["HTTPS_PORT"] is not null
    || app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Liveness + database readiness. Anonymous on purpose: the Docker healthcheck and any
// upstream probe have no credentials.
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
