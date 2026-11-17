using EmployeeManagement.DTOs;
using EmployeeManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Tests;

public class CommandServiceTests
{
    [Fact]
    public async Task SalaryUpdate_RollsBackWhenAttendanceInsertFails()
    {
        using var db = new TestDb();
        int employeeId;
        decimal originalSalary;
        await using (var context = db.CreateDbContext())
        {
            var catalog = await SampleCatalog.SeedAsync(context);
            employeeId = catalog.RiyaId;
            originalSalary = context.Employees.Single(employee => employee.EmployeeId == employeeId).BaseSalary!.Value;
        }

        var service = new EmployeeCommandService(db, NullLogger<EmployeeCommandService>.Instance);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSalaryWithAttendanceAsync(new SalaryUpdateRequest
        {
            EmployeeId = employeeId,
            BaseSalary = originalSalary + 500,
            AttendanceDate = new DateOnly(2026, 4, 1),
            CheckInTime = new TimeOnly(8, 0),
            SimulateFailure = true
        }));

        Assert.Equal("Simulated attendance failure.", exception.Message);
        await using var check = db.CreateDbContext();
        var employee = await check.Employees.SingleAsync(item => item.EmployeeId == employeeId);
        Assert.Equal(originalSalary, employee.BaseSalary);
        Assert.Empty(check.Attendances.Where(attendance => attendance.EmployeeId == employeeId && attendance.AttendanceDate == new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task SalaryUpdate_SavesSalaryAndAttendanceTogether()
    {
        using var db = new TestDb();
        int employeeId;
        await using (var context = db.CreateDbContext())
        {
            employeeId = (await SampleCatalog.SeedAsync(context)).DevikaId;
        }

        var service = new EmployeeCommandService(db, NullLogger<EmployeeCommandService>.Instance);
        await service.UpdateSalaryWithAttendanceAsync(new SalaryUpdateRequest
        {
            EmployeeId = employeeId,
            BaseSalary = 5200,
            AttendanceDate = new DateOnly(2026, 4, 2),
            CheckInTime = new TimeOnly(8, 5),
            SimulateFailure = false
        });

        await using var check = db.CreateDbContext();
        var employee = await check.Employees.SingleAsync(item => item.EmployeeId == employeeId);
        Assert.Equal(5200, employee.BaseSalary);
        Assert.Contains(check.Attendances, attendance =>
            attendance.EmployeeId == employeeId && attendance.AttendanceDate == new DateOnly(2026, 4, 2));
    }

    [Fact]
    public async Task SecondConcurrentUpdate_IsRejected()
    {
        using var db = new TestDb();
        int employeeId;
        await using (var context = db.CreateDbContext())
        {
            employeeId = (await SampleCatalog.SeedAsync(context)).RiyaId;
        }

        var result = await new EmployeeCommandService(db, NullLogger<EmployeeCommandService>.Instance)
            .DemonstrateConcurrencyAsync(employeeId, 2, 5);

        Assert.Equal(UserMessages.Concurrency, result.SecondUpdateMessage);
        await using var check = db.CreateDbContext();
        var employee = await check.Employees.SingleAsync(item => item.EmployeeId == employeeId);
        Assert.Equal(2, employee.PerformanceRating);
    }
}
