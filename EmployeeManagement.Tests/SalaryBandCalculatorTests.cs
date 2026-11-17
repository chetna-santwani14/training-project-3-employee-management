using EmployeeManagement.DTOs;
using EmployeeManagement.Services;

namespace EmployeeManagement.Tests;

public class SalaryBandCalculatorTests
{
    [Fact]
    public void Boundaries_GoToTheHigherBand()
    {
        var salaries = new decimal[] { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000 };
        var inputs = salaries.Select((salary, index) => new SalaryInput
        {
            EmployeeId = index + 1,
            EmployeeName = "E" + (index + 1),
            Salary = salary
        }).ToList();

        var report = SalaryBandCalculator.Classify(inputs);
        var bySalary = report.Bands.ToDictionary(band => band.Salary);

        Assert.Equal("Low", bySalary[1000].Band);
        Assert.Equal(0, bySalary[1000].Percentile);
        Assert.Equal("Mid", bySalary[4000].Band);
        Assert.Equal(30, bySalary[4000].Percentile);
        Assert.Equal("High", bySalary[8000].Band);
        Assert.Equal(70, bySalary[8000].Percentile);
        Assert.Equal(inputs.Count, report.Bands.Count);
    }

    [Fact]
    public void EqualSalaries_ShareOnePercentile_AndNullSalaryIsExcluded()
    {
        var report = SalaryBandCalculator.Classify(
        [
            new SalaryInput { EmployeeId = 1, EmployeeName = "A", Salary = 100 },
            new SalaryInput { EmployeeId = 2, EmployeeName = "B", Salary = 100 },
            new SalaryInput { EmployeeId = 3, EmployeeName = "C", Salary = null }
        ]);

        Assert.Equal(report.Bands[0].Percentile, report.Bands[1].Percentile);
        Assert.Equal("Low", report.Bands[0].Band);
        Assert.Equal("C", Assert.Single(report.MissingSalary).EmployeeName);
    }
}
