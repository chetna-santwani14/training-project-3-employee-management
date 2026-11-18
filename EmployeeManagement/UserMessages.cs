namespace EmployeeManagement;

public static class UserMessages
{
    public const string InvalidOption = "That isn't on the menu. Try again.";
    public const string EmployeeNotFound = "No employee with that id.";
    public const string InvalidNumber = "Need a whole number there.";
    public const string PageTooSmall = "Page has to be 1 or higher.";
    public const string UnsupportedOrder = "I only know FirstName, LastName, and HireDate.";
    public const string UnsupportedGrouping = "Grouping is department, role, or both.";
    public const string InvalidDateRange = "The from date is after the to date.";
    public const string InvalidDate = "Date should look like 2026-03-02.";
    public const string InvalidTime = "Time should look like 09:15.";
    public const string SalaryPositive = "Salary has to be more than zero.";
    public const string RatingRange = "Rating is 1 to 5.";
    public const string UnknownStatus = "Status has to be Active, Completed, Maintenance, or On Hold.";
    public const string Concurrency = "Someone else already saved this employee. Load it again and retry.";
    public const string Saved = "Saved.";
    public const string RolledBack = "Rolled back. Salary and attendance are unchanged.";
    public const string SystemError = "That didn't work. Check the database connection and try again.";
    public const string NoAssignments = "Nobody has hours logged on a project yet.";
}
