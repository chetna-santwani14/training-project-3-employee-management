namespace EmployeeManagement.Models;

public partial class Employee
{
    /// <summary>Code First column. Null until HR records a rating. Allowed range is 1-5.</summary>
    public int? PerformanceRating { get; set; }

    /// <summary>Optimistic concurrency token. SQL Server generates this value.</summary>
    public byte[] RowVersion { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
