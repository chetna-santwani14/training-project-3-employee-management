namespace EmployeeManagement.Models;

public partial class Employee
{
    // Added with a migration. Null until someone records a rating (1-5).
    public int? PerformanceRating { get; set; }

    // SQL Server fills this in. Used so two people can't overwrite the same row.
    public byte[] RowVersion { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
