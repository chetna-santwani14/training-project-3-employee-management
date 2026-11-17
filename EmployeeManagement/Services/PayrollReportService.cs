using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class PayrollReportService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public PayrollReportService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<PayrollGroupDto>> GetGroupedPayrollAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        var selected = keys.Select(key => key.ToLowerInvariant()).ToHashSet();
        if (selected.Count == 0)
        {
            selected.Add("department");
            selected.Add("role");
        }

        if (selected.Any(key => key is not ("department" or "role")))
        {
            throw new ArgumentException(UserMessages.UnsupportedGrouping);
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = context.Employees.AsNoTracking().Select(employee => new
        {
            DepartmentName = employee.Department.DepartmentName,
            RoleName = employee.Role != null ? employee.Role.RoleName : "Unassigned",
            Bonus = (double?)employee.Bonus ?? 0d,
            NetPay = (double?)employee.NetPay
        });

        IQueryable<PayrollGroupDto> grouped;
        if (selected.Contains("department") && selected.Contains("role"))
        {
            grouped = rows.GroupBy(row => new { row.DepartmentName, row.RoleName })
                .Select(group => new PayrollGroupDto
                {
                    DepartmentName = group.Key.DepartmentName,
                    RoleName = group.Key.RoleName,
                    TotalBonus = (decimal)group.Sum(row => row.Bonus),
                    AverageNetPay = (decimal?)group.Average(row => row.NetPay)
                });
        }
        else if (selected.Contains("department"))
        {
            grouped = rows.GroupBy(row => row.DepartmentName)
                .Select(group => new PayrollGroupDto
                {
                    DepartmentName = group.Key,
                    RoleName = "(all)",
                    TotalBonus = (decimal)group.Sum(row => row.Bonus),
                    AverageNetPay = (decimal?)group.Average(row => row.NetPay)
                });
        }
        else
        {
            grouped = rows.GroupBy(row => row.RoleName)
                .Select(group => new PayrollGroupDto
                {
                    DepartmentName = "(all)",
                    RoleName = group.Key,
                    TotalBonus = (decimal)group.Sum(row => row.Bonus),
                    AverageNetPay = (decimal?)group.Average(row => row.NetPay)
                });
        }

        return await grouped
            .OrderBy(group => group.DepartmentName)
            .ThenBy(group => group.RoleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleAggregateDto>> GetRoleAggregatesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var query =
            from employee in context.Employees.AsNoTracking()
            group employee by employee.Role != null ? employee.Role.RoleName : "Unassigned" into roleGroup
            orderby roleGroup.Key
            select new RoleAggregateDto
            {
                RoleName = roleGroup.Key,
                TotalBonus = (decimal)roleGroup.Sum(employee => (double?)employee.Bonus ?? 0d),
                AverageNetPay = (decimal?)roleGroup.Average(employee => (double?)employee.NetPay),
                ProjectCount = roleGroup
                    .SelectMany(employee => employee.EmployeeProjects)
                    .Select(assignment => assignment.ProjectId)
                    .Distinct()
                    .Count()
            };

        return await query.ToListAsync(cancellationToken);
    }
}
