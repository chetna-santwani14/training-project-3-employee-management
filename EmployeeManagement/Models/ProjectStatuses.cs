namespace EmployeeManagement.Models;

public static class ProjectStatuses
{
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Maintenance = "Maintenance";
    public const string OnHold = "On Hold";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        Active,
        Completed,
        Maintenance,
        OnHold
    };
}
