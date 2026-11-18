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
                Console.WriteLine("Bye.");
                return;
            }

            await RunSafeAsync(() => RunOptionAsync(option, cancellationToken));
            Console.WriteLine();
            Console.WriteLine("Hit Enter to go back to the menu.");
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
        Console.WriteLine("Already applied:");
        foreach (var name in status.Applied)
        {
            Console.WriteLine("  " + name);
        }

        if (status.Applied.Count == 0)
        {
            Console.WriteLine("  (none)");
        }

        Console.WriteLine("Still pending:");
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
            ? "PerformanceRating is in the database. Old rows stay null until someone sets a rating."
            : "PerformanceRating migration hasn't been applied yet.");
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
            Console.WriteLine("Left out, no salary on file:");
            foreach (var employee in report.MissingSalary)
            {
                Console.WriteLine("  " + employee.EmployeeName);
            }
        }
    }

    private async Task ShowGroupingAsync(CancellationToken cancellationToken)
    {
        Console.Write("Group by department, role, or department,role (just press Enter for both): ");
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
        Console.Write("Which page? ");
        if (!InputValidator.TryParsePage(Console.ReadLine(), out var page))
        {
            Console.WriteLine(UserMessages.PageTooSmall);
            return;
        }

        Console.Write("Sort by FirstName, LastName, or HireDate: ");
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
        Console.WriteLine($"Showing page {pageResult.Page} of {pageResult.TotalPages} ({pageResult.TotalCount} people)");
    }

    private async Task ShowChecksAsync(CancellationToken cancellationToken)
    {
        Console.Write("Which day? (yyyy-MM-dd): ");
        if (!InputValidator.TryParseDate(Console.ReadLine(), out var date))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        var result = await _employees.RunChecksAsync(date, cancellationToken);
        Console.WriteLine($"Did every Sales person check in before 9? {result.SalesCheckResult}");
        Console.WriteLine($"Is there an Intern? {(result.InternExists ? "Yes" : "No")}");
        Console.WriteLine($"Does \"Revamp Website\" exist? {(result.RevampWebsiteExists ? "Yes" : "No")}");
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
        Console.WriteLine("Project names, no duplicates:");
        foreach (var name in directory.UniqueProjectNames)
        {
            Console.WriteLine("  " + name);
        }
    }

    private async Task ShowExpressionFilterAsync(CancellationToken cancellationToken)
    {
        Console.Write("Department id (leave blank to skip): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var departmentId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Role id (leave blank to skip): ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var roleId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Project status, or blank (Active, Completed, Maintenance, On Hold): ");
        if (!InputValidator.TryParseProjectStatus(Console.ReadLine(), out var status))
        {
            Console.WriteLine(UserMessages.UnknownStatus);
            return;
        }

        WriteSummaries(await _filters.FilterWithExpressionsAsync(departmentId, roleId, status, cancellationToken));
    }

    private async Task ShowPredicateFilterAsync(CancellationToken cancellationToken)
    {
        Console.Write("Department name (blank skips it): ");
        var department = EmptyToNull(Console.ReadLine());
        Console.Write("Role name (blank skips it): ");
        var role = EmptyToNull(Console.ReadLine());
        Console.Write("Hired from (yyyy-MM-dd, blank skips it): ");
        if (!InputValidator.TryParseOptionalDate(Console.ReadLine(), out var from))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        Console.Write("Hired to (yyyy-MM-dd, blank skips it): ");
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
        Console.Write("Employee id: ");
        if (!InputValidator.TryParsePositiveInt(Console.ReadLine(), out var employeeId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("New salary: ");
        if (!InputValidator.TryParseSalary(Console.ReadLine(), out var salary))
        {
            Console.WriteLine(UserMessages.SalaryPositive);
            return;
        }

        Console.Write("Check-in date (yyyy-MM-dd): ");
        if (!InputValidator.TryParseDate(Console.ReadLine(), out var date))
        {
            Console.WriteLine(UserMessages.InvalidDate);
            return;
        }

        Console.Write("What time did they check in? (HH:mm): ");
        if (!InputValidator.TryParseTime(Console.ReadLine(), out var time))
        {
            Console.WriteLine(UserMessages.InvalidTime);
            return;
        }

        Console.Write("Pretend the attendance insert fails so we can see the rollback? (y/n): ");
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
        Console.WriteLine("Normal list (Maintenance people are hidden):");
        foreach (var name in demo.WithFilter)
        {
            Console.WriteLine("  " + name);
        }

        Console.WriteLine("Same list with IgnoreQueryFilters():");
        foreach (var name in demo.WithoutFilter)
        {
            Console.WriteLine("  " + name);
        }
    }

    private async Task ShowConcurrencyAsync(CancellationToken cancellationToken)
    {
        Console.Write("Employee id: ");
        if (!InputValidator.TryParsePositiveInt(Console.ReadLine(), out var employeeId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("First rating (1-5). This one gets saved: ");
        if (!InputValidator.TryParseRating(Console.ReadLine(), out var first))
        {
            Console.WriteLine(UserMessages.RatingRange);
            return;
        }

        Console.Write("Second rating (1-5). This one should lose the race: ");
        if (!InputValidator.TryParseRating(Console.ReadLine(), out var second))
        {
            Console.WriteLine(UserMessages.RatingRange);
            return;
        }

        var result = await _commands.DemonstrateConcurrencyAsync(employeeId, first, second, cancellationToken);
        Console.WriteLine($"First save stuck. Rating is now {result.FirstValueSaved}.");
        Console.WriteLine(result.SecondUpdateMessage);
    }

    private async Task ShowStoredProcedureAsync(CancellationToken cancellationToken)
    {
        Console.Write("Department id, or blank for every department: ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var departmentId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Role id, or blank for every role: ");
        if (!InputValidator.TryParseOptionalPositiveInt(Console.ReadLine(), out var roleId))
        {
            Console.WriteLine(UserMessages.InvalidNumber);
            return;
        }

        Console.Write("Sort by FirstName, LastName, HireDate, or DepartmentName (Enter = HireDate): ");
        var sortByInput = Console.ReadLine();
        Console.Write("ASC or DESC (Enter = ASC): ");
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
        Console.WriteLine("Employee Management");
        Console.WriteLine(" 1  Who is staffed on each project?");
        Console.WriteLine(" 2  Did the PerformanceRating column get migrated?");
        Console.WriteLine(" 3  Who logged the most hours? (ties included)");
        Console.WriteLine(" 4  First, last, and 5th hire in each department");
        Console.WriteLine(" 5  Salary bands (low / mid / high)");
        Console.WriteLine(" 6  Bonus and net pay, grouped how you like");
        Console.WriteLine(" 7  Employees, 10 per page");
        Console.WriteLine(" 8  Sales check-in, intern, Revamp Website");
        Console.WriteLine(" 9  Projects for each person, plus the unique names");
        Console.WriteLine("10  Bonus, average net pay, and project count by role");
        Console.WriteLine("11  Everyone, even people with no project");
        Console.WriteLine("12  Filter built with an expression tree");
        Console.WriteLine("13  Filter built with a predicate builder");
        Console.WriteLine("14  Completed projects and net pay over 5000");
        Console.WriteLine("15  Change a salary and add attendance (one transaction)");
        Console.WriteLine("16  Maintenance people, with and without the filter");
        Console.WriteLine("17  Two contexts, one employee");
        Console.WriteLine("18  The employee / department / role view");
        Console.WriteLine("19  GetEmployeesByDepartment");
        Console.WriteLine(" 0  Exit");
        Console.Write("What do you want to look at? ");
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
