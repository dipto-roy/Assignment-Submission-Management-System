using System.Text;
using System.Text.Json.Serialization;
using AssignmentSubmissionSystem.Api.Configuration;
using AssignmentSubmissionSystem.Api.Middleware;
using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Auth;
using AssignmentSubmissionSystem.Application.Classes;
using AssignmentSubmissionSystem.Application.Options;
using AssignmentSubmissionSystem.Application.Subjects;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Users;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;
using AssignmentSubmissionSystem.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.MigrateAsync(db);

    if (app.Environment.IsDevelopment())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, passwordHasher);
    }
}

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
