using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Services;

public class EmployeeCommandService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;
    private readonly ILogger<EmployeeCommandService> _logger;

    public EmployeeCommandService(
        IDbContextFactory<EmployeeDbContext> factory,
        ILogger<EmployeeCommandService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task UpdateSalaryWithAttendanceAsync(
        SalaryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.BaseSalary <= 0)
        {
            throw new ArgumentException(UserMessages.SalaryPositive);
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var employee = await context.Employees
                    .FirstOrDefaultAsync(item => item.EmployeeId == request.EmployeeId, cancellationToken);
                if (employee is null)
                {
                    throw new InvalidOperationException(UserMessages.EmployeeNotFound);
                }

                employee.BaseSalary = request.BaseSalary;
                await context.SaveChangesAsync(cancellationToken);

                if (request.SimulateFailure)
                {
                    throw new InvalidOperationException("Simulated attendance failure.");
                }

                context.Attendances.Add(new Attendance
                {
                    EmployeeId = employee.EmployeeId,
                    AttendanceDate = request.AttendanceDate,
                    CheckInTime = request.CheckInTime
                });
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Salary and attendance committed for employee {EmployeeId}.", employee.EmployeeId);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                context.ChangeTracker.Clear();
                _logger.LogWarning(exception, "Salary and attendance transaction rolled back for employee {EmployeeId}.", request.EmployeeId);
                throw;
            }
        });
    }

    public async Task<ConcurrencyDemoResult> DemonstrateConcurrencyAsync(
        int employeeId,
        int firstRating,
        int secondRating,
        CancellationToken cancellationToken = default)
    {
        if (firstRating is < 1 or > 5 || secondRating is < 1 or > 5)
        {
            throw new ArgumentException(UserMessages.RatingRange);
        }

        await using var firstContext = await _factory.CreateDbContextAsync(cancellationToken);
        await using var secondContext = await _factory.CreateDbContextAsync(cancellationToken);

        var firstCopy = await firstContext.Employees
            .FirstOrDefaultAsync(employee => employee.EmployeeId == employeeId, cancellationToken);
        var secondCopy = await secondContext.Employees
            .FirstOrDefaultAsync(employee => employee.EmployeeId == employeeId, cancellationToken);

        if (firstCopy is null || secondCopy is null)
        {
            throw new InvalidOperationException(UserMessages.EmployeeNotFound);
        }

        firstCopy.PerformanceRating = firstRating;
        secondCopy.PerformanceRating = secondRating;

        await firstContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Concurrency demo stored the first update for employee {EmployeeId}.", employeeId);

        try
        {
            await secondContext.SaveChangesAsync(cancellationToken);
            return new ConcurrencyDemoResult
            {
                FirstValueSaved = firstRating.ToString(),
                SecondUpdateMessage = "The second update was stored."
            };
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Concurrent update rejected for employee {EmployeeId}.", employeeId);
            return new ConcurrencyDemoResult
            {
                FirstValueSaved = firstRating.ToString(),
                SecondUpdateMessage = UserMessages.Concurrency
            };
        }
    }
}
