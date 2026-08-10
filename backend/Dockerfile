# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files first for layer-cached restore.
# Only the Api project graph is restored/published here — the test projects are
# excluded by .dockerignore and are not part of the runtime image.
# Directory.Build.props supplies TargetFramework and friends, and .editorconfig backs
# EnforceCodeStyleInBuild, so both must be present before restore.
COPY Directory.Build.props .editorconfig ./
COPY src/Api/AssignmentSubmissionSystem.Api.csproj src/Api/
COPY src/Application/AssignmentSubmissionSystem.Application.csproj src/Application/
COPY src/Domain/AssignmentSubmissionSystem.Domain.csproj src/Domain/
COPY src/Infrastructure/AssignmentSubmissionSystem.Infrastructure.csproj src/Infrastructure/

RUN dotnet restore src/Api/AssignmentSubmissionSystem.Api.csproj

# Copy remaining source and publish
COPY . .
RUN dotnet publish src/Api/AssignmentSubmissionSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AssignmentSubmissionSystem.Api.dll"]
