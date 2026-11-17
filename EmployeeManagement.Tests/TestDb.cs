using EmployeeManagement.Data;
using EmployeeManagement.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Tests;

internal sealed class TestDb : IDbContextFactory<EmployeeDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<EmployeeDbContext> _options;

    public TestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public EmployeeDbContext CreateDbContext() => new(_options);

    public Task<EmployeeDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}

internal sealed class SampleCatalog
{
    public int RiyaId { get; init; }
    public int ArjunId { get; init; }
    public int AnanyaId { get; init; }
    public int DevikaId { get; init; }
    public int SureshId { get; init; }
    public int EngineeringId { get; init; }
    public int DeveloperRoleId { get; init; }

    public static async Task<SampleCatalog> SeedAsync(EmployeeDbContext context)
    {
        var engineering = new Department { DepartmentName = "Engineering" };
        var sales = new Department { DepartmentName = "Sales" };
        var finance = new Department { DepartmentName = "Finance" };
        var operations = new Department { DepartmentName = "Operations" };
        context.Departments.AddRange(engineering, sales, finance, operations);

        var developer = new Role { RoleName = "Developer" };
        var intern = new Role { RoleName = "Intern" };
        var manager = new Role { RoleName = "Manager" };
        context.Roles.AddRange(developer, intern, manager);

        var revamp = new Project { ProjectName = "Revamp Website", Status = ProjectStatuses.Active };
        var mobile = new Project { ProjectName = "Mobile App", Status = ProjectStatuses.Completed };
        var maintenance = new Project { ProjectName = "Legacy Maintenance", Status = ProjectStatuses.Maintenance };
        var empty = new Project { ProjectName = "Empty Pilot", Status = ProjectStatuses.OnHold };
        context.Projects.AddRange(revamp, mobile, maintenance, empty);
        await context.SaveChangesAsync();

        var riya = Employee(engineering, developer, "Riya", "Patel", new DateOnly(2018, 1, 10), 3000, 100, 2800);
        var arjun = Employee(engineering, developer, "Arjun", "Mehta", new DateOnly(2018, 1, 10), 3500, 200, 3200);
        var neha = Employee(engineering, manager, "Neha", "Gupta", new DateOnly(2019, 3, 1), 4000, null, 6100);
        var vikram = Employee(engineering, developer, "Vikram", "Singh", new DateOnly(2020, 6, 15), 4500, 150, 4500);
        var ananya = Employee(engineering, intern, "Ananya", "Rao", new DateOnly(2021, 2, 1), 5000, 0, 4800);
        var kabir = Employee(engineering, developer, "Kabir", "Shah", new DateOnly(2022, 11, 20), 8000, 300, 9000);
        var sara = Employee(sales, manager, "Sara", "Khan", new DateOnly(2017, 5, 1), 7000, 400, 6400);
        var rohan = Employee(sales, developer, "Rohan", "Das", new DateOnly(2020, 8, 12), 9000, 250, 7600);
        var devika = Employee(finance, manager, "Devika", "Rao", new DateOnly(2019, 12, 1), 4800, 80, 4300);
        var suresh = Employee(engineering, developer, "Suresh", "Iyer", new DateOnly(2023, 6, 1), 10000, 100, 9500);
        var extra = Employee(finance, developer, "Extra", "One", new DateOnly(2024, 5, 1), 5100, 10, 4900);
        var secondExtra = Employee(finance, intern, "Extra", "Two", new DateOnly(2024, 6, 1), 5300, 10, 5000);
        context.Employees.AddRange(riya, arjun, neha, vikram, ananya, kabir, sara, rohan, devika, suresh, extra, secondExtra);

        context.EmployeeProjects.AddRange(
            Assignment(riya, revamp, developer, 40),
            Assignment(riya, mobile, developer, 30),
            Assignment(arjun, mobile, developer, 70),
            Assignment(neha, mobile, manager, 20),
            Assignment(vikram, revamp, developer, null),
            Assignment(ananya, revamp, intern, 5),
            Assignment(kabir, mobile, developer, 10),
            Assignment(sara, revamp, manager, 12),
            Assignment(rohan, revamp, developer, 8),
            Assignment(suresh, maintenance, developer, 200),
            Assignment(suresh, revamp, developer, 15));

        context.Attendances.AddRange(
            new Attendance { Employee = sara, AttendanceDate = new DateOnly(2026, 3, 2), CheckInTime = new TimeOnly(8, 15) },
            new Attendance { Employee = rohan, AttendanceDate = new DateOnly(2026, 3, 2), CheckInTime = new TimeOnly(8, 40) },
            new Attendance { Employee = sara, AttendanceDate = new DateOnly(2026, 3, 3), CheckInTime = new TimeOnly(8, 10) },
            new Attendance { Employee = rohan, AttendanceDate = new DateOnly(2026, 3, 3), CheckInTime = new TimeOnly(9, 30) });

        await context.SaveChangesAsync();

        return new SampleCatalog
        {
            RiyaId = riya.EmployeeId,
            ArjunId = arjun.EmployeeId,
            AnanyaId = ananya.EmployeeId,
            DevikaId = devika.EmployeeId,
            SureshId = suresh.EmployeeId,
            EngineeringId = engineering.DepartmentId,
            DeveloperRoleId = developer.RoleId
        };
    }

    private static Employee Employee(
        Department department,
        Role role,
        string firstName,
        string lastName,
        DateOnly hireDate,
        decimal salary,
        decimal? bonus,
        decimal netPay)
        => new()
        {
            Department = department,
            Role = role,
            FirstName = firstName,
            LastName = lastName,
            HireDate = hireDate,
            BaseSalary = salary,
            Bonus = bonus,
            NetPay = netPay
        };

    private static EmployeeProject Assignment(Employee employee, Project project, Role role, decimal? hours)
        => new()
        {
            Employee = employee,
            Project = project,
            Role = role,
            HoursWorked = hours
        };
}
