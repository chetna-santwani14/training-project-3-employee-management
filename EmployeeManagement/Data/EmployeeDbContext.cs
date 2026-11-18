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

        // Hide anyone sitting on a Maintenance project. Raw SQL, the view, and the procedure
        // repeat this check themselves because they skip the filter. Don't add the same
        // filter on EmployeeProject — it loops back into this one and the query blows up.
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

    // SQL Server writes rowversion itself. The SQLite tests stamp a new value so the
    // concurrency demo still fails the second save.
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
