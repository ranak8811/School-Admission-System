# ==============================================================================
# Multi-stage Dockerfile for School Admission System Backend (ASP.NET Core 8)
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimal layer caching
COPY SchoolAdmissionSystem.sln ./
COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
COPY tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj tests/SchoolAdmission.Tests/

# Restore NuGet dependencies
RUN dotnet restore

# Copy all remaining source files
COPY . .

# Publish optimized Release binaries
WORKDIR /src/src/SchoolAdmission.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Lightweight Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Set default production environment and listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
