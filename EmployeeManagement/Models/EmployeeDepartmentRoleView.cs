namespace EmployeeManagement.Models;

/// <summary>Keyless mapping for dbo.vw_EmployeeDepartmentRole.</summary>
public class EmployeeDepartmentRoleView
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
}
