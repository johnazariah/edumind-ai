using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AcademicAssessment.Infrastructure.Services;

/// <summary>
/// Service for provisioning and managing per-school databases
/// Implements physical database isolation for B2B schools
/// </summary>
public interface ISchoolDatabaseProvisioner
{
    /// <summary>
    /// Provisions a new database for a school
    /// Creates database, applies schema, stores connection string in Key Vault
    /// </summary>
    Task<Result<Unit>> ProvisionSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection string for a school from Key Vault
    /// </summary>
    Task<Result<string>> GetSchoolConnectionStringAsync(
        School school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs migrations on a school database
    /// </summary>
    Task<Result<Unit>> MigrateSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a school database (for school offboarding)
    /// </summary>
    Task<Result<Unit>> DeleteSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of school database provisioner
/// </summary>
public sealed class SchoolDatabaseProvisioner : ISchoolDatabaseProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly SecretClient _keyVaultClient;
    private readonly string _postgresAdminConnectionString;

    public SchoolDatabaseProvisioner(IConfiguration configuration)
    {
        _configuration = configuration;

        // Get Key Vault URL from configuration
        var keyVaultUrl = _configuration["Azure:KeyVault:Url"]
            ?? throw new InvalidOperationException("Azure:KeyVault:Url not configured");

        // Initialize Key Vault client with managed identity
        _keyVaultClient = new SecretClient(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());

        // Get admin connection string for creating databases
        _postgresAdminConnectionString = _configuration.GetConnectionString("PostgresAdmin")
            ?? throw new InvalidOperationException("PostgresAdmin connection string not configured");
    }

    public async Task<Result<Unit>> ProvisionSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Create database
            var createDbResult = await CreateDatabaseAsync(school, cancellationToken);
            if (createDbResult.IsFailure)
                return createDbResult;

            // Step 2: Create connection string for the new database
            var connectionString = BuildSchoolConnectionString(school);

            // Step 3: Store connection string in Azure Key Vault
            var storeResult = await StoreConnectionStringInKeyVaultAsync(
                school,
                connectionString,
                cancellationToken);

            if (storeResult.IsFailure)
                return storeResult;

            // Step 4: Apply schema migrations
            var migrateResult = await MigrateSchoolDatabaseAsync(school, cancellationToken);
            if (migrateResult.IsFailure)
                return migrateResult;

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex, "PROVISIONING_FAILED");
        }
    }

    public async Task<Result<string>> GetSchoolConnectionStringAsync(
        School school,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await _keyVaultClient.GetSecretAsync(
                school.ConnectionStringKey,
                cancellationToken: cancellationToken);

            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return Error.NotFound("Connection string", school.ConnectionStringKey);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public async Task<Result<Unit>> MigrateSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionStringResult = await GetSchoolConnectionStringAsync(school, cancellationToken);
            if (connectionStringResult.IsFailure)
            {
                if (connectionStringResult is Result<string>.Failure failure &&
                    failure.Error.Code == "NOT_FOUND")
                {
                    // Connection string doesn't exist, create it
                    var connectionString = BuildSchoolConnectionString(school);
                    await StoreConnectionStringInKeyVaultAsync(school, connectionString, cancellationToken);
                    connectionStringResult = connectionString;
                }
                else
                {
                    return connectionStringResult.Match<Result<Unit>>(
                        _ => Unit.Value,
                        error => error);
                }
            }

            var connectionString = connectionStringResult.GetValueOrThrow();

            // Apply migrations using EF Core or SQL scripts
            // For now, we'll just create the schema
            await CreateSchemaAsync(connectionString, cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public async Task<Result<Unit>> DeleteSchoolDatabaseAsync(
        School school,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Delete from Key Vault
            await _keyVaultClient.StartDeleteSecretAsync(
                school.ConnectionStringKey,
                cancellationToken);

            // Step 2: Drop database
            await using var connection = new NpgsqlConnection(_postgresAdminConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{school.DatabaseName}\";",
                connection);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    private async Task<Result<Unit>> CreateDatabaseAsync(
        School school,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_postgresAdminConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if database already exists
            await using var checkCommand = new NpgsqlCommand(
                $"SELECT 1 FROM pg_database WHERE datname = '{school.DatabaseName}';",
                connection);

            var exists = await checkCommand.ExecuteScalarAsync(cancellationToken);
            if (exists is not null)
            {
                return Error.Conflict($"Database {school.DatabaseName} already exists");
            }

            // Create database
            await using var createCommand = new NpgsqlCommand(
                $"CREATE DATABASE \"{school.DatabaseName}\" WITH ENCODING 'UTF8';",
                connection);

            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    private async Task<Result<Unit>> StoreConnectionStringInKeyVaultAsync(
        School school,
        string connectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            await _keyVaultClient.SetSecretAsync(
                school.ConnectionStringKey,
                connectionString,
                cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    private string BuildSchoolConnectionString(School school)
    {
        var builder = new NpgsqlConnectionStringBuilder(_postgresAdminConnectionString)
        {
            Database = school.DatabaseName
        };

        return builder.ToString();
    }

    private async Task CreateSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        // This would normally use EF Core migrations
        // For now, we'll create a basic schema
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var schemaSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                email VARCHAR(255) NOT NULL UNIQUE,
                full_name VARCHAR(255) NOT NULL,
                role INT NOT NULL,
                school_id UUID,
                is_active BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL,
                external_id VARCHAR(255)
            );

            CREATE TABLE IF NOT EXISTS students (
                id UUID PRIMARY KEY,
                user_id UUID NOT NULL UNIQUE,
                school_id UUID,
                class_ids TEXT NOT NULL,
                grade_level INT NOT NULL,
                date_of_birth DATE,
                parental_consent_granted BOOLEAN NOT NULL DEFAULT FALSE,
                parent_email VARCHAR(255),
                subscription_tier INT NOT NULL DEFAULT 0,
                subscription_expires_at TIMESTAMPTZ,
                level INT NOT NULL DEFAULT 1,
                xp_points INT NOT NULL DEFAULT 0,
                daily_streak INT NOT NULL DEFAULT 0,
                last_activity_date DATE,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS classes (
                id UUID PRIMARY KEY,
                school_id UUID NOT NULL,
                name VARCHAR(255) NOT NULL,
                code VARCHAR(50) NOT NULL,
                grade_level INT NOT NULL,
                subject INT NOT NULL,
                teacher_ids TEXT NOT NULL,
                student_ids TEXT NOT NULL,
                academic_year VARCHAR(20) NOT NULL,
                is_active BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            -- Additional tables would be created here...
        ";

        await using var command = new NpgsqlCommand(schemaSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
