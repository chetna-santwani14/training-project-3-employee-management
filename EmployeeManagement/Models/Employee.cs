namespace EmployeeManagement.Models;

// Matches the Employee table from the starter script.
// Extra columns live in Employee.Custom.cs so a re-scaffold doesn't wipe them.
public partial class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public int DepartmentId { get; set; }
    public int? RoleId { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? Bonus { get; set; }
    public decimal? NetPay { get; set; }

    public Department Department { get; set; } = null!;
    public Role? Role { get; set; }
    public ICollection<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
