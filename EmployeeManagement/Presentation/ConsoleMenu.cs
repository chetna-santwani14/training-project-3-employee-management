using EmployeeManagement.DTOs;
using EmployeeManagement.Services;
using EmployeeManagement.Validators;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Presentation;

public class ConsoleMenu
{
    private readonly ProjectReportService _projects;
    private readonly EmployeeReportService _employees;
    private readonly PayrollReportService _payroll;
    private readonly EmployeeFilterService _filters;
    private readonly RawSqlReportService _rawSql;
    private readonly EmployeeCommandService _commands;
    private readonly DatabaseFeatureService _database;
    private readonly ILogger<ConsoleMenu> _logger;

    public ConsoleMenu(
        ProjectReportService projects,
        EmployeeReportService employees,
        PayrollReportService payroll,
        EmployeeFilterService filters,
        RawSqlReportService rawSql,
        EmployeeCommandService commands,
        DatabaseFeatureService database,
        ILogger<ConsoleMenu> logger)
    {
        _projects = projects;
        _employees = employees;
        _payroll = payroll;
        _filters = filters;
        _rawSql = rawSql;
        _commands = commands;
        _database = database;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            PrintMenu();
            var choice = Console.ReadLine();
            if (!InputValidator.TryParseMenuOption(choice, out var option))
            {
                Console.WriteLine(UserMessages.InvalidOption);
                continue;
            }

            if (option == 0)
            {
                Console.WriteLine("Goodbye.");
                return;
            }

            await RunSafeAsync(() => RunOptionAsync(option, cancellationToken));
            Console.WriteLine();
            Console.WriteLine("Press Enter to return to the menu.");
            Console.ReadLine();
        }
    }

    private async Task RunOptionAsync(int option, CancellationToken cancellationToken)
    {
        switch (option)
        {
            case 1:
                WriteTable(
                    ["Project", "Employee", "Role"],
                    (await _projects.GetProjectStaffingAsync(cancellationToken))
                        .Select(row => new[] { row.ProjectName, Blank(row.EmployeeName), Blank(row.RoleName) }));
                break;
            case 2:
                await ShowMigrationsAsync(cancellationToken);
                break;
            case 3:
                await ShowHighestHoursAsync(cancellationToken);
                break;
            case 4:
                WriteTable(
                    ["Department", "First hired", "Last hired", "5th hired"],
                    (await _employees.GetHireOrderByDepartmentAsync(cancellationToken))
                        .Select(row => new[]
                        {
                            row.DepartmentName,
                            row.FirstHired ?? "N/A",
                            row.LastHired ?? "N/A",
                            row.FifthHired ?? "N/A"
                        }));
                break;
            case 5:
                await ShowSalaryBandsAsync(cancellationToken);
                break;
            case 6:
                await ShowGroupingAsync(cancellationToken);
                break;
            case 7:
                await ShowPageAsync(cancellationToken);
                break;
            case 8:
                await ShowChecksAsync(cancellationToken);
                break;
            case 9:
                await ShowProjectDirectoryAsync(cancellationToken);
                break;
            case 10:
                WriteTable(
                    ["Role", "Total bonus", "Average NetPay", "Projects"],
                    (await _payroll.GetRoleAggregatesAsync(cancellationToken))
                        .Select(row => new[]
                        {
                            row.RoleName ?? "Unassigned",
                            row.TotalBonus.ToString("0.00"),
                            row.AverageNetPay?.ToString("0.00") ?? "n/a",
                            row.ProjectCount.ToString()
                        }));
                break;
            case 11:
                WriteTable(
                    ["Employee", "Project", "Role"],
                    (await _employees.GetEmployeesWithAssignmentsAsync(cancellationToken))
                        .Select(row => new[]
                        {
                            row.EmployeeName,
                            row.ProjectName ?? "Unassigned",
                            row.RoleName ?? "Unassigned"
                        }));
                break;
            case 12:
                await ShowExpressionFilterAsync(cancellationToken);
                break;
            case 13:
                await ShowPredicateFilterAsync(cancellationToken);
                break;
            case 14:
                WriteTable(
                    ["EmployeeId", "First name", "Last name", "NetPay", "Role"],
                    (await _rawSql.GetCompletedProjectHighPaidAsync(cancellationToken))
                        .Select(row => new[]
                        {
                            row.EmployeeId.ToString(),
                            row.FirstName,
                            row.LastName,
                            row.NetPay.ToString("0.00"),
                            row.RoleName ?? "Unassigned"
                        }));
                break;
            case 15:
                await UpdateSalaryAsync(cancellationToken);
                break;
            case 16:
                await ShowFilterDemoAsync(cancellationToken);
                break;
            case 17:
                await ShowConcurrencyAsync(cancellationToken);
                break;
            case 18:
                WriteTable(
                    ["EmployeeId", "First name", "Last name", "Department", "Role"],
                    (await _database.GetEmployeeViewAsync(cancellationToken))
                        .Select(row => new[]
                        {
                            row.EmployeeId.ToString(),
                            row.FirstName,
                            row.LastName,
                            row.DepartmentName,
                            row.RoleName ?? "Unassigned"
                        }));
                break;
            case 19:
                await ShowStoredProcedureAsync(cancellationToken);
                break;
        }
    }

    private async Task ShowMigrationsAsync(CancellationToken cancellationToken)
    {
        var status = await _database.GetMigrationStatusAsync(cancellationToken);
        Console.WriteLine("Applied migrations:");
        foreach (var name in status.Applied)
        {
            Console.WriteLine("  " + name);
        }

        if (status.Applied.Count == 0)
        {
            Console.WriteLine("  (none)");
        }

        Console.WriteLine("Pending migrations:");
        foreach (var name in status.Pending)
        {
            Console.WriteLine("  " + name);
        }

        if (status.Pending.Count == 0)
        {
            Console.WriteLine("  (none)");
        }

        var ratingApplied = status.Applied.Any(name =>
            name.Contains("PerformanceRating", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine(ratingApplied
            ? "PerformanceRating migration is applied. Existing employees keep a null rating until it is set."
            : "PerformanceRating migration is still pending.");
    }

    private async Task ShowHighestHoursAsync(CancellationToken cancellationToken)
    {
        var rows = await _employees.GetHighestHoursEmployeesAsync(cancellationToken);
        if (rows.Count == 0)
        {
            Console.WriteLine(UserMessages.NoAssignments);
            return;
        }

        WriteTable(
            ["EmployeeId", "Employee", "Total hours"],
            rows.Select(row => new[] { row.EmployeeId.ToString(), row.EmployeeName, row.TotalHours.ToString("0.00") }));
    }

    private async Task ShowSalaryBandsAsync(CancellationToken cancellationToken)
    {
        var report = await _employees.GetSalaryBandsAsync(cancellationToken);
        WriteTable(
            ["Employee", "Salary", "Percentile", "Band"],
            report.Bands.Select(row => new[]
            {
                row.EmployeeName,
                row.Salary.ToString("0.00"),
                row.Percentile.ToString("0.00"),
                row.Band
            }));

        if (report.MissingSalary.Count > 0)
        {
            Console.WriteLine("Excluded because salary is missing:");
            foreach (var employee in report.MissingSalary)
            {
                Console.WriteLine("  " + employee.EmployeeName);
            }
        }
    }

    private async Task ShowGroupingAsync(CancellationToken cancellationToken)
    {
        Console.Write("Grouping keys (department, role, or department,role). Enter for both: ");
        if (!InputValidator.TryParseGroupingKeys(Console.ReadLine(), out var keys))
        {
            Console.WriteLine(UserMessages.UnsupportedGrouping);
            return;
        }

        WriteTable(
            ["Department", "Role", "Total bonus", "Average NetPay"],
            (await _payroll.GetGroupedPayrollAsync(keys, cancellationToken))
                .Select(row => new[]
                {
                    row.DepartmentName,
                    row.RoleName,
                    row.TotalBonus.ToString("0.00"),
                    row.AverageNetPay?.ToString("0.00") ?? "n/a"
                }));
    }

    private async Task ShowPageAsync(CancellationToken cancellationToken)
    {
        Console.Write("Page number: ");
        if (!InputValidator.TryParsePage(Console.ReadLine(), out var page))
        {
            Console.WriteLine(UserMessages.PageTooSmall);
            return;
        }

        Console.Write("Order by (FirstName, LastName, HireDate): ");
        if (!InputValidator.TryParseOrderField(Console.ReadLine(), out var orderBy))
        {
            Console.WriteLine(UserMessages.UnsupportedOrder);
            return;
        }

        var pageResult = await _employees.GetEmployeesPageAsync(page, orderBy, cancellationToken);
        WriteTable(
            ["EmployeeId", "First name", "Last name", "Hire date", "Department", "Role"],
            pageResult.Items.Select(row => new[]
            {
                row.EmployeeId.ToString(),
                row.FirstName,
                row.LastName,
                row.HireDate.ToString("yyyy-MM-dd"),
                row.DepartmentName,
                row.RoleName
            }));
        Console.WriteLine($"Page {pageResult.Page} of {pageResult.TotalPages} ({pageResult.TotalCount} employees)");
    }

    private async Task ShowChecksAsync(CancellationToken cancellationToken)
    {
        Console.Write("Date (yyyy-MM-dd): ");
        if (!InputValidator.TryParseDate(Console.ReadLine(), out var date))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        var result = await _employees.RunChecksAsync(date, cancellationToken);
        Console.WriteLine($"All Sales employees checked in before 09:00: {result.SalesCheckResult}");
        Console.WriteLine($"Any Intern exists: {(result.InternExists ? "Yes" : "No")}");
        Console.WriteLine($"Project \"Revamp Website\" exists: {(result.RevampWebsiteExists ? "Yes" : "No")}");
    }

    private async Task ShowProjectDirectoryAsync(CancellationToken cancellationToken)
    {
        var directory = await _projects.GetProjectsPerEmployeeAsync(cancellationToken);
        foreach (var employee in directory.Employees)
        {
            var projects = employee.Projects.Count == 0 ? "(none)" : string.Join(", ", employee.Projects);
            Console.WriteLine($"{employee.EmployeeName}: {projects}");
        }

        Console.WriteLine();
        Console.WriteLine("Unique project names:");
        foreach (var name in directory.UniqueProjectNames)
        {
            Console.WriteLine("  " + name);
        }
    }

    private async Task ShowExpressionFilterAsync(CancellationToken cancellationToken)
    {
        Console.Write("DepartmentId (blank to skip): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var departmentId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("RoleId (blank to skip): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var roleId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Project status (Active, Completed, Maintenance, On Hold, or blank): ");
        if (!InputValidator.TryParseProjectStatus(Console.ReadLine(), out var status))
        {
            Console.WriteLine(UserMessages.UnknownStatus);
            return;
        }

        WriteSummaries(await _filters.FilterWithExpressionsAsync(departmentId, roleId, status, cancellationToken));
    }

    private async Task ShowPredicateFilterAsync(CancellationToken cancellationToken)
    {
        Console.Write("Department name (blank to skip): ");
        var department = EmptyToNull(Console.ReadLine());
        Console.Write("Role name (blank to skip): ");
        var role = EmptyToNull(Console.ReadLine());
        Console.Write("Hire date from (yyyy-MM-dd, blank to skip): ");
        if (!InputValidator.TryParseOptionalDate(Console.ReadLine(), out var from))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        Console.Write("Hire date to (yyyy-MM-dd, blank to skip): ");
        if (!InputValidator.TryParseOptionalDate(Console.ReadLine(), out var to))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        if (!InputValidator.TryParseDateRange(from, to))
        {
            Console.WriteLine(UserMessages.InvalidDateRange);
            return;
        }

        WriteSummaries(await _filters.FilterWithPredicateAsync(department, role, from, to, cancellationToken));
    }

    private async Task UpdateSalaryAsync(CancellationToken cancellationToken)
    {
        Console.Write("EmployeeId: ");
        if (!InputValidator.TryParsePositiveInt(Console.ReadLine(), out var employeeId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("New base salary: ");
        if (!InputValidator.TryParseSalary(Console.ReadLine(), out var salary))
        {
            Console.WriteLine(UserMessages.SalaryPositive);
            return;
        }

        Console.Write("Attendance date (yyyy-MM-dd): ");
        if (!InputValidator.TryParseDate(Console.ReadLine(), out var date))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        Console.Write("Check-in time (HH:mm): ");
        if (!InputValidator.TryParseTime(Console.ReadLine(), out var time))
        {
            Console.WriteLine(UserMessages.InvalidTime);
            return;
        }

        Console.Write("Simulate a failure after the salary change? (y/n): ");
        var simulate = string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);

        try
        {
            await _commands.UpdateSalaryWithAttendanceAsync(new SalaryUpdateRequest
            {
                EmployeeId = employeeId,
                BaseSalary = salary,
                AttendanceDate = date,
                CheckInTime = time,
                SimulateFailure = simulate
            }, cancellationToken);
            Console.WriteLine(UserMessages.Saved);
        }
        catch (InvalidOperationException exception) when (exception.Message == "Simulated attendance failure.")
        {
            Console.WriteLine(UserMessages.RolledBack);
        }
    }

    private async Task ShowFilterDemoAsync(CancellationToken cancellationToken)
    {
        var demo = await _database.GetMaintenanceFilterDemoAsync(cancellationToken);
        Console.WriteLine("Standard queries (Maintenance employees hidden):");
        foreach (var name in demo.WithFilter)
        {
            Console.WriteLine("  " + name);
        }

        Console.WriteLine("IgnoreQueryFilters():");
        foreach (var name in demo.WithoutFilter)
        {
            Console.WriteLine("  " + name);
        }
    }

    private async Task ShowConcurrencyAsync(CancellationToken cancellationToken)
    {
        Console.Write("EmployeeId: ");
        if (!InputValidator.TryParsePositiveInt(Console.ReadLine(), out var employeeId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("First PerformanceRating (1-5): ");
        if (!InputValidator.TryParseRating(Console.ReadLine(), out var first))
        {
            Console.WriteLine(UserMessages.RatingRange);
            return;
        }

        Console.Write("Second PerformanceRating (1-5): ");
        if (!InputValidator.TryParseRating(Console.ReadLine(), out var second))
        {
            Console.WriteLine(UserMessages.RatingRange);
            return;
        }

        var result = await _commands.DemonstrateConcurrencyAsync(employeeId, first, second, cancellationToken);
        Console.WriteLine($"First update stored rating {result.FirstValueSaved}.");
        Console.WriteLine(result.SecondUpdateMessage);
    }

    private async Task ShowStoredProcedureAsync(CancellationToken cancellationToken)
    {
        Console.Write("DepartmentId (blank for all): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var departmentId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("RoleId (blank for all): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var roleId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Sort by (FirstName, LastName, HireDate, DepartmentName). Enter for HireDate: ");
        var sortByInput = Console.ReadLine();
        Console.Write("Direction (ASC or DESC). Enter for ASC: ");
        if (!InputValidator.TryParseSort(sortByInput, Console.ReadLine(), out var sortBy, out var direction))
        {
            Console.WriteLine(UserMessages.UnsupportedOrder);
            return;
        }

        WriteTable(
            ["Employee", "Department", "Role", "Hire date", "Tenure", "Projects", "Total"],
            (await _database.GetEmployeesByDepartmentSpAsync(departmentId, roleId, sortBy, direction, cancellationToken))
                .Select(row => new[]
                {
                    $"{row.FirstName} {row.LastName}",
                    row.DepartmentName,
                    row.RoleName ?? "Unassigned",
                    row.HireDate.ToString("yyyy-MM-dd"),
                    row.TenureYears.ToString(),
                    row.ProjectCount.ToString(),
                    row.TotalRows.ToString()
                }));
    }

    private async Task RunSafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ArgumentException exception)
        {
            _logger.LogInformation(exception, "Validation failed.");
            Console.WriteLine(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogInformation(exception, "Request rejected.");
            Console.WriteLine(exception.Message);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Concurrent update rejected.");
            Console.WriteLine(UserMessages.Concurrency);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Database update failed.");
            Console.WriteLine(UserMessages.SystemError);
        }
        catch (SqlException exception)
        {
            _logger.LogError(exception, "SQL Server error.");
            Console.WriteLine(UserMessages.SystemError);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error.");
            Console.WriteLine(UserMessages.SystemError);
        }
    }

    private static void WriteSummaries(IReadOnlyList<EmployeeSummaryDto> rows)
    {
        WriteTable(
            ["EmployeeId", "Employee", "Department", "Role", "Hire date"],
            rows.Select(row => new[]
            {
                row.EmployeeId.ToString(),
                row.EmployeeName,
                row.DepartmentName,
                row.RoleName,
                row.HireDate.ToString("yyyy-MM-dd")
            }));
    }

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("Employee Management System");
        Console.WriteLine("1  Project staffing report");
        Console.WriteLine("2  PerformanceRating migration status");
        Console.WriteLine("3  Highest working hours");
        Console.WriteLine("4  Hire order per department");
        Console.WriteLine("5  Salary bands");
        Console.WriteLine("6  Dynamic payroll grouping");
        Console.WriteLine("7  Paged employees");
        Console.WriteLine("8  Condition checks");
        Console.WriteLine("9  Projects per employee");
        Console.WriteLine("10 Role aggregates");
        Console.WriteLine("11 Employees with assignments");
        Console.WriteLine("12 Expression-tree filter");
        Console.WriteLine("13 Predicate Builder filter");
        Console.WriteLine("14 Completed projects, NetPay above 5000");
        Console.WriteLine("15 Update salary and attendance");
        Console.WriteLine("16 Maintenance filter demo");
        Console.WriteLine("17 Concurrency demo");
        Console.WriteLine("18 Employee department role view");
        Console.WriteLine("19 Stored procedure");
        Console.WriteLine("0  Exit");
        Console.Write("Choose an option: ");
    }

    private static void WriteTable(IReadOnlyList<string> headers, IEnumerable<string[]> rows)
    {
        var data = rows.ToList();
        var widths = headers.Select(header => header.Length).ToArray();
        foreach (var row in data)
        {
            for (var index = 0; index < widths.Length; index++)
            {
                widths[index] = Math.Max(widths[index], row[index].Length);
            }
        }

        Console.WriteLine(Format(headers.ToArray(), widths));
        Console.WriteLine(string.Join("-+-", widths.Select(width => new string('-', width))));
        foreach (var row in data)
        {
            Console.WriteLine(Format(row, widths));
        }

        if (data.Count == 0)
        {
            Console.WriteLine("(no rows)");
        }
    }

    private static string Format(IReadOnlyList<string> cells, int[] widths)
        => string.Join(" | ", cells.Select((cell, index) => cell.PadRight(widths[index])));

    private static string Blank(string value) => string.IsNullOrEmpty(value) ? "(none)" : value;

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
