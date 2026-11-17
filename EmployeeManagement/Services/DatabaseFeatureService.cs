using System.Data;
using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class DatabaseFeatureService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public DatabaseFeatureService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<MigrationStatusDto> GetMigrationStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        return new MigrationStatusDto
        {
            Applied = applied.ToList(),
            Pending = pending.ToList()
        };
    }

    public async Task<FilterDemoDto> GetMaintenanceFilterDemoAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var withFilter = await context.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.EmployeeId)
            .Select(employee => employee.FirstName + " " + employee.LastName)
            .ToListAsync(cancellationToken);

        var withoutFilter = await context.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(employee => employee.EmployeeId)
            .Select(employee => employee.FirstName + " " + employee.LastName)
            .ToListAsync(cancellationToken);

        return new FilterDemoDto
        {
            WithFilter = withFilter,
            WithoutFilter = withoutFilter
        };
    }

    public async Task<IReadOnlyList<Models.EmployeeDepartmentRoleView>> GetEmployeeViewAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        return await context.EmployeeDepartmentRoles
            .AsNoTracking()
            .OrderBy(row => row.EmployeeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeSpResult>> GetEmployeesByDepartmentSpAsync(
        int? departmentId,
        int? roleId,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetEmployeesByDepartment";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@DepartmentId", (object?)departmentId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@RoleId", (object?)roleId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@SortBy", sortBy));
            command.Parameters.Add(new SqlParameter("@SortDirection", sortDirection));

            var results = new List<EmployeeSpResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new EmployeeSpResult
                {
                    EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                    RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString(reader.GetOrdinal("RoleName")),
                    HireDate = reader.GetDateTime(reader.GetOrdinal("HireDate")),
                    TenureYears = reader.GetInt32(reader.GetOrdinal("TenureYears")),
                    ProjectCount = reader.GetInt32(reader.GetOrdinal("ProjectCount")),
                    TotalRows = reader.GetInt32(reader.GetOrdinal("TotalRows"))
                });
            }

            return results;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
