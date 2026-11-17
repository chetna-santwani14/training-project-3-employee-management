using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Data;

public class EmployeeDbContext : DbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EmployeeProject> EmployeeProjects => Set<EmployeeProject>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<EmployeeDepartmentRoleView> EmployeeDepartmentRoles => Set<EmployeeDepartmentRoleView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeDbContext).Assembly);

        // Employees assigned to at least one Maintenance project are hidden from normal queries.
        // The same predicate is repeated in the raw SQL, the view, and the stored procedure,
        // because those bypass this filter. A second filter on EmployeeProject loops back into
        // this one and overflows the query pipeline, so the rule stays on Employee only.
        modelBuilder.Entity<Employee>().HasQueryFilter(employee =>
            !employee.EmployeeProjects.Any(assignment => assignment.Project.Status == ProjectStatuses.Maintenance));

        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Employee>().ToTable(table => table.HasCheckConstraint(
                "CK_Employee_PerformanceRating",
                "[PerformanceRating] IS NULL OR ([PerformanceRating] >= 1 AND [PerformanceRating] <= 5)"));

            modelBuilder.Entity<Employee>().Property(employee => employee.RowVersion).IsRowVersion();
        }
        else
        {
            modelBuilder.Entity<Employee>().Property(employee => employee.RowVersion)
                .IsConcurrencyToken()
                .HasColumnType("BLOB")
                .ValueGeneratedNever();
        }
    }

    public override int SaveChanges()
    {
        StampRowVersionsForNonSqlServer();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampRowVersionsForNonSqlServer();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// SQL Server generates rowversion. The SQLite tests used by this project stamp a new token
    /// so optimistic concurrency can be demonstrated without SQL Server.
    /// </summary>
    private void StampRowVersionsForNonSqlServer()
    {
        if (Database.IsSqlServer())
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<Employee>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(employee => employee.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
