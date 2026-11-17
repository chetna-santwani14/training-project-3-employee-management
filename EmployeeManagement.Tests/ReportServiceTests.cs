using EmployeeManagement.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Tests;

public class ReportServiceTests
{
    [Fact]
    public async Task StaffingReport_IncludesAnEmptyProject_AndHidesMaintenanceEmployees()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var rows = await new ProjectReportService(db).GetProjectStaffingAsync();

        Assert.Contains(rows, row => row.ProjectName == "Empty Pilot" && row.EmployeeName == "");
        Assert.DoesNotContain(rows, row => row.EmployeeName == "Suresh Iyer");
        Assert.Contains(rows, row => row.ProjectName == "Revamp Website" && row.EmployeeName == "Riya Patel" && row.RoleName == "Developer");
    }

    [Fact]
    public async Task HighestHours_ReturnsEveryEmployeeTiedForTheMaximum()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var winners = await new EmployeeReportService(db).GetHighestHoursEmployeesAsync();

        Assert.Equal(70, Assert.Single(winners.Select(winner => winner.TotalHours).Distinct()));
        Assert.Equal(["Arjun Mehta", "Riya Patel"], winners.Select(winner => winner.EmployeeName).OrderBy(name => name));
    }

    [Fact]
    public async Task HireOrder_ShowsFifthOnlyWhenTheDepartmentHasFiveEmployees()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var rows = await new EmployeeReportService(db).GetHireOrderByDepartmentAsync();
        var engineering = Assert.Single(rows, row => row.DepartmentName == "Engineering");
        var sales = Assert.Single(rows, row => row.DepartmentName == "Sales");
        var operations = Assert.Single(rows, row => row.DepartmentName == "Operations");

        Assert.Equal("Riya Patel", engineering.FirstHired);
        Assert.Equal("Kabir Shah", engineering.LastHired);
        Assert.Equal("Ananya Rao", engineering.FifthHired);
        Assert.Null(sales.FifthHired);
        Assert.Null(operations.FirstHired);
        Assert.Null(operations.FifthHired);
    }

    [Fact]
    public async Task SameHireDate_IsBrokenByEmployeeId()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var engineering = (await new EmployeeReportService(db).GetHireOrderByDepartmentAsync())
            .Single(row => row.DepartmentName == "Engineering");

        Assert.Equal("Riya Patel", engineering.FirstHired);
        Assert.NotEqual("Neha Gupta", engineering.FirstHired);
    }

    [Fact]
    public async Task Assignments_IncludeEmployeesWithNoProject()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var rows = await new EmployeeReportService(db).GetEmployeesWithAssignmentsAsync();
        var devika = Assert.Single(rows, row => row.EmployeeName == "Devika Rao");

        Assert.Null(devika.ProjectName);
        Assert.Null(devika.RoleName);
        Assert.DoesNotContain(rows, row => row.EmployeeName == "Suresh Iyer");
    }

    [Fact]
    public async Task Paging_ReturnsTenRows_AndAnEmptyPagePastTheEnd()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var service = new EmployeeReportService(db);
        var page = await service.GetEmployeesPageAsync(2, "HireDate");
        var beyond = await service.GetEmployeesPageAsync(9, "FirstName");

        Assert.True(page.Items.Count <= 10);
        Assert.NotEmpty(page.Items);
        Assert.Empty(beyond.Items);
        Assert.True(page.TotalCount > 10);
    }

    [Fact]
    public async Task ConditionChecks_DistinguishOnTime_Late_AndMissingSalesStaff()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var service = new EmployeeReportService(db);
        var onTime = await service.RunChecksAsync(new DateOnly(2026, 3, 2));
        var late = await service.RunChecksAsync(new DateOnly(2026, 3, 3));

        Assert.Equal("Yes", onTime.SalesCheckResult);
        Assert.Equal("No", late.SalesCheckResult);
        Assert.True(onTime.InternExists);
        Assert.True(onTime.RevampWebsiteExists);
    }

    [Fact]
    public async Task UniqueProjectNames_AreNotRepeated()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var directory = await new ProjectReportService(db).GetProjectsPerEmployeeAsync();
        var devika = Assert.Single(directory.Employees, employee => employee.EmployeeName == "Devika Rao");

        Assert.Empty(devika.Projects);
        Assert.Equal(directory.UniqueProjectNames.Count, directory.UniqueProjectNames.Distinct().Count());
        Assert.DoesNotContain("Legacy Maintenance", directory.UniqueProjectNames);
    }

    [Fact]
    public async Task PayrollGrouping_TreatsNullBonusAsZero()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var groups = await new PayrollReportService(db).GetGroupedPayrollAsync(["department", "role"]);
        var nehaGroup = Assert.Single(groups, group => group.DepartmentName == "Engineering" && group.RoleName == "Manager");

        Assert.Equal(0, nehaGroup.TotalBonus);
        Assert.Equal(6100, nehaGroup.AverageNetPay);
    }

    [Fact]
    public async Task GlobalFilter_HidesMaintenanceEmployees_UntilItIsIgnored()
    {
        using var db = new TestDb();
        await using (var context = db.CreateDbContext())
        {
            await SampleCatalog.SeedAsync(context);
        }

        var demo = await new DatabaseFeatureService(db).GetMaintenanceFilterDemoAsync();

        Assert.DoesNotContain("Suresh Iyer", demo.WithFilter);
        Assert.Contains("Suresh Iyer", demo.WithoutFilter);
    }

    [Fact]
    public async Task ExpressionFilter_RunsAgainstTheDatabase()
    {
        using var db = new TestDb();
        SampleCatalog catalog;
        await using (var context = db.CreateDbContext())
        {
            catalog = await SampleCatalog.SeedAsync(context);
        }

        var service = new EmployeeFilterService(db);
        var all = await service.FilterWithExpressionsAsync(null, null, null);
        var engineeringDevelopers = await service.FilterWithExpressionsAsync(catalog.EngineeringId, catalog.DeveloperRoleId, null);

        Assert.DoesNotContain(all, employee => employee.EmployeeName == "Suresh Iyer");
        Assert.All(engineeringDevelopers, employee => Assert.Equal("Engineering", employee.DepartmentName));
        Assert.Contains(engineeringDevelopers, employee => employee.EmployeeName == "Riya Patel");
    }
}
