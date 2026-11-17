namespace EmployeeManagement.DTOs;

public sealed class ProjectStaffingDto
{
    public string ProjectName { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}

public sealed class HighestHoursDto
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public decimal TotalHours { get; init; }
}

public sealed class HireOrderDto
{
    public string DepartmentName { get; init; } = string.Empty;
    public string? FirstHired { get; init; }
    public string? LastHired { get; init; }
    public string? FifthHired { get; init; }
}

public sealed class SalaryInput
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public decimal? Salary { get; init; }
}

public sealed class SalaryBandDto
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public decimal Salary { get; init; }
    public double Percentile { get; init; }
    public string Band { get; init; } = string.Empty;
}

public sealed class SalaryBandReport
{
    public IReadOnlyList<SalaryBandDto> Bands { get; init; } = [];
    public IReadOnlyList<SalaryInput> MissingSalary { get; init; } = [];
}

public sealed class PayrollGroupDto
{
    public string DepartmentName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public decimal TotalBonus { get; init; }
    public decimal? AverageNetPay { get; init; }
}

public sealed class EmployeeListDto
{
    public int EmployeeId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class ConditionCheckDto
{
    public required string SalesCheckResult { get; init; }
    public required bool InternExists { get; init; }
    public required bool RevampWebsiteExists { get; init; }
}

public sealed class EmployeeProjectsDto
{
    public required string EmployeeName { get; init; }
    public required IReadOnlyList<string> Projects { get; init; }
}

public sealed class ProjectDirectoryDto
{
    public required IReadOnlyList<EmployeeProjectsDto> Employees { get; init; }
    public required IReadOnlyList<string> UniqueProjectNames { get; init; }
}

public sealed class RoleAggregateDto
{
    public string? RoleName { get; init; }
    public decimal TotalBonus { get; init; }
    public decimal? AverageNetPay { get; init; }
    public int ProjectCount { get; init; }
}

public sealed class EmployeeAssignmentDto
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string? ProjectName { get; init; }
    public string? RoleName { get; init; }
}

public sealed class EmployeeSummaryDto
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
}

public sealed class EmployeeNetPayDto
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal NetPay { get; set; }
    public string? RoleName { get; set; }
}

public sealed class SalaryUpdateRequest
{
    public required int EmployeeId { get; init; }
    public required decimal BaseSalary { get; init; }
    public required DateOnly AttendanceDate { get; init; }
    public required TimeOnly CheckInTime { get; init; }
    public bool SimulateFailure { get; init; }
}

public sealed class FilterDemoDto
{
    public required IReadOnlyList<string> WithFilter { get; init; }
    public required IReadOnlyList<string> WithoutFilter { get; init; }
}

public sealed class ConcurrencyDemoResult
{
    public required string FirstValueSaved { get; init; }
    public required string SecondUpdateMessage { get; init; }
}

public sealed class EmployeeSpResult
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public DateTime HireDate { get; set; }
    public int TenureYears { get; set; }
    public int ProjectCount { get; set; }
    public int TotalRows { get; set; }
}

public sealed class MigrationStatusDto
{
    public required IReadOnlyList<string> Applied { get; init; }
    public required IReadOnlyList<string> Pending { get; init; }
}
