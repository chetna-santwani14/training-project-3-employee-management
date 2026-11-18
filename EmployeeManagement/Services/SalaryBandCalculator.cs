using EmployeeManagement.DTOs;

namespace EmployeeManagement.Services;

public static class SalaryBandCalculator
{
    public static SalaryBandReport Classify(IEnumerable<SalaryInput> employees)
    {
        var all = employees.ToList();
        var paid = all.Where(employee => employee.Salary is not null)
            .OrderBy(employee => employee.Salary)
            .ThenBy(employee => employee.EmployeeId)
            .ToList();
        var total = paid.Count;

        var bands = paid.Select(employee =>
        {
            // Share of people paid strictly less. Same salary stays in the same band.
            var lower = paid.Count(other => other.Salary < employee.Salary);
            var percentile = total == 0 ? 0 : (double)lower / total * 100;
            var band = percentile < 30 ? "Low" : percentile < 70 ? "Mid" : "High";
            return new SalaryBandDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.EmployeeName,
                Salary = employee.Salary!.Value,
                Percentile = Math.Round(percentile, 2),
                Band = band
            };
        }).ToList();

        return new SalaryBandReport
        {
            Bands = bands,
            MissingSalary = all.Where(employee => employee.Salary is null).ToList()
        };
    }
}
