using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class RawSqlReportService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public RawSqlReportService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<EmployeeNetPayDto>> GetCompletedProjectHighPaidAsync(
        CancellationToken cancellationToken = default)
    {
        const string status = ProjectStatuses.Completed;
        const decimal minimumNetPay = 5000m;

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        // SqlQuery parameterizes the interpolated values. The NOT EXISTS clause repeats the
        // Maintenance rule because raw SQL does not apply the EF global query filter.
        var rows = await context.Database.SqlQuery<EmployeeNetPayDto>($@"
            SELECT DISTINCT
                e.EmployeeId,
                e.FirstName,
                e.LastName,
                e.NetPay,
                r.RoleName
            FROM Employee AS e
            INNER JOIN EmployeeProject AS ep ON ep.EmployeeId = e.EmployeeId
            INNER JOIN Project AS p ON p.ProjectId = ep.ProjectId
            LEFT JOIN Role AS r ON r.RoleId = e.RoleId
            WHERE p.Status = {status}
              AND e.NetPay > {minimumNetPay}
              AND NOT EXISTS (
                  SELECT 1
                  FROM EmployeeProject AS hiddenAssignment
                  INNER JOIN Project AS hiddenProject ON hiddenProject.ProjectId = hiddenAssignment.ProjectId
                  WHERE hiddenAssignment.EmployeeId = e.EmployeeId
                    AND hiddenProject.Status = {ProjectStatuses.Maintenance}
              )")
            .ToListAsync(cancellationToken);

        return rows.OrderBy(row => row.EmployeeId).ToList();
    }
}
