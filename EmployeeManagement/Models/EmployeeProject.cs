namespace EmployeeManagement.Models;

public class EmployeeProject
{
    public int EmployeeProjectId { get; set; }
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public int RoleId { get; set; }
    public decimal? HoursWorked { get; set; }

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
