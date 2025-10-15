using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AcademicAssessment.Infrastructure.Data;

/// <summary>
/// Design-time factory for AcademicContext to support EF Core migrations.
/// This is only used during design-time operations like 'dotnet ef migrations add' and 'dotnet ef database update'.
/// </summary>
public class AcademicContextFactory : IDesignTimeDbContextFactory<AcademicContext>
{
    public AcademicContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AcademicContext>();

        // Use connection string from environment or default to development database
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password;Include Error Detail=true";

        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly("AcademicAssessment.Infrastructure"));

        // Create a null tenant context for design-time operations (migrations don't need tenant filtering)
        var tenantContext = new DesignTimeTenantContext();

        return new AcademicContext(optionsBuilder.Options, tenantContext);
    }

    /// <summary>
    /// Simple tenant context for design-time operations.
    /// Returns default/null values since migrations don't need tenant filtering.
    /// </summary>
    private class DesignTimeTenantContext : AcademicAssessment.Core.Interfaces.ITenantContext
    {
        public Guid UserId => Guid.Empty;
        public AcademicAssessment.Core.Enums.UserRole Role => AcademicAssessment.Core.Enums.UserRole.SystemAdmin;
        public Guid? SchoolId => null;
        public IReadOnlyList<Guid> ClassIds => Array.Empty<Guid>();
        public string Email => "design-time@localhost";
        public string FullName => "Design Time User";

        public bool HasAccessToSchool(Guid schoolId) => true;
        public bool HasAccessToClass(Guid classId) => true;
        public bool HasRole(AcademicAssessment.Core.Enums.UserRole minimumRole) => true;
    }
}
