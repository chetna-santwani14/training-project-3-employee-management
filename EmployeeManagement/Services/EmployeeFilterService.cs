using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using EmployeeManagement.Filters;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class EmployeeFilterService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public EmployeeFilterService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<EmployeeSummaryDto>> FilterWithExpressionsAsync(
        int? departmentId,
        int? roleId,
        string? projectStatus,
        CancellationToken cancellationToken = default)
    {
        var predicate = EmployeeExpressionFilter.Build(departmentId, roleId, projectStatus);
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await Project(context.Employees.AsNoTracking().Where(predicate), cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeSummaryDto>> FilterWithPredicateAsync(
        string? departmentName,
        string? roleName,
        DateOnly? hireFrom,
        DateOnly? hireTo,
        CancellationToken cancellationToken = default)
    {
        var predicate = EmployeePredicateFilter.Build(departmentName, roleName, hireFrom, hireTo);
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await Project(context.Employees.AsNoTracking().Where(predicate), cancellationToken);
    }

    private static async Task<IReadOnlyList<EmployeeSummaryDto>> Project(
        IQueryable<Models.Employee> query,
        CancellationToken cancellationToken)
    {
        return await query
            .OrderBy(employee => employee.EmployeeId)
            .Select(employee => new EmployeeSummaryDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FirstName + " " + employee.LastName,
                DepartmentName = employee.Department.DepartmentName,
                RoleName = employee.Role != null ? employee.Role.RoleName : "Unassigned",
                HireDate = employee.HireDate
            })
            .ToListAsync(cancellationToken);
    }
}
