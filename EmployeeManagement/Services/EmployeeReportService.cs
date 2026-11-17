using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class EmployeeReportService
{
    public const int PageSize = 10;
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public EmployeeReportService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<HighestHoursDto>> GetHighestHoursEmployeesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var totals = context.Employees
            .AsNoTracking()
            .Where(employee => employee.EmployeeProjects.Any())
            .Select(employee => new
            {
                employee.EmployeeId,
                EmployeeName = employee.FirstName + " " + employee.LastName,
                TotalHours = employee.EmployeeProjects.Sum(assignment => (double?)assignment.HoursWorked) ?? 0d
            });

        var maximum = await totals.MaxAsync(row => (double?)row.TotalHours, cancellationToken);
        if (maximum is null)
        {
            return [];
        }

        var winners = await totals
            .Where(row => row.TotalHours == maximum)
            .OrderBy(row => row.EmployeeName)
            .ToListAsync(cancellationToken);

        return winners.Select(row => new HighestHoursDto
        {
            EmployeeId = row.EmployeeId,
            EmployeeName = row.EmployeeName,
            TotalHours = (decimal)row.TotalHours
        }).ToList();
    }

    public async Task<IReadOnlyList<HireOrderDto>> GetHireOrderByDepartmentAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var query =
            from department in context.Departments.AsNoTracking()
            orderby department.DepartmentName
            select new HireOrderDto
            {
                DepartmentName = department.DepartmentName,
                FirstHired = department.Employees
                    .OrderBy(employee => employee.HireDate)
                    .ThenBy(employee => employee.EmployeeId)
                    .Select(employee => employee.FirstName + " " + employee.LastName)
                    .FirstOrDefault(),
                LastHired = department.Employees
                    .OrderByDescending(employee => employee.HireDate)
                    .ThenByDescending(employee => employee.EmployeeId)
                    .Select(employee => employee.FirstName + " " + employee.LastName)
                    .FirstOrDefault(),
                FifthHired = department.Employees
                    .OrderBy(employee => employee.HireDate)
                    .ThenBy(employee => employee.EmployeeId)
                    .Skip(4)
                    .Select(employee => employee.FirstName + " " + employee.LastName)
                    .FirstOrDefault()
            };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<SalaryBandReport> GetSalaryBandsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var employees = await context.Employees
            .AsNoTracking()
            .Select(employee => new SalaryInput
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FirstName + " " + employee.LastName,
                Salary = employee.BaseSalary
            })
            .ToListAsync(cancellationToken);

        return SalaryBandCalculator.Classify(employees);
    }

    public async Task<PagedResult<EmployeeListDto>> GetEmployeesPageAsync(
        int page,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentException(UserMessages.PageTooSmall);
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var employees = context.Employees.AsNoTracking();

        employees = orderBy switch
        {
            "FirstName" => employees.OrderBy(employee => employee.FirstName).ThenBy(employee => employee.EmployeeId),
            "LastName" => employees.OrderBy(employee => employee.LastName).ThenBy(employee => employee.EmployeeId),
            "HireDate" => employees.OrderBy(employee => employee.HireDate).ThenBy(employee => employee.EmployeeId),
            _ => throw new ArgumentException(UserMessages.UnsupportedOrder)
        };

        var total = await employees.CountAsync(cancellationToken);
        var items = await employees
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(employee => new EmployeeListDto
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                HireDate = employee.HireDate,
                DepartmentName = employee.Department.DepartmentName,
                RoleName = employee.Role != null ? employee.Role.RoleName : "Unassigned"
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListDto>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = total
        };
    }

    public async Task<ConditionCheckDto> RunChecksAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var sales = context.Employees.AsNoTracking()
            .Where(employee => employee.Department.DepartmentName == "Sales");
        var salesCount = await sales.CountAsync(cancellationToken);
        string salesResult;
        if (salesCount == 0)
        {
            salesResult = "No Sales employees";
        }
        else
        {
            var cutoff = new TimeOnly(9, 0);
            var allOnTime = await sales.AllAsync(
                employee => employee.Attendances.Any(attendance =>
                    attendance.AttendanceDate == date && attendance.CheckInTime < cutoff),
                cancellationToken);
            salesResult = allOnTime ? "Yes" : "No";
        }

        var internExists = await context.Employees.AsNoTracking()
            .AnyAsync(employee => employee.Role != null && employee.Role.RoleName == "Intern", cancellationToken);
        var revampExists = await context.Projects.AsNoTracking()
            .AnyAsync(project => project.ProjectName == "Revamp Website", cancellationToken);

        return new ConditionCheckDto
        {
            SalesCheckResult = salesResult,
            InternExists = internExists,
            RevampWebsiteExists = revampExists
        };
    }

    public async Task<IReadOnlyList<EmployeeAssignmentDto>> GetEmployeesWithAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var query =
            from employee in context.Employees.AsNoTracking()
            join assignment in context.EmployeeProjects.AsNoTracking()
                on employee.EmployeeId equals assignment.EmployeeId into assignments
            from assignment in assignments.DefaultIfEmpty()
            orderby employee.EmployeeId
            select new EmployeeAssignmentDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FirstName + " " + employee.LastName,
                ProjectName = assignment == null ? null : assignment.Project.ProjectName,
                RoleName = assignment == null ? null : assignment.Role.RoleName
            };

        return await query.ToListAsync(cancellationToken);
    }
}
