namespace EmployeeManagement.Models;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public TimeOnly CheckInTime { get; set; }

    public Employee Employee { get; set; } = null!;
}
