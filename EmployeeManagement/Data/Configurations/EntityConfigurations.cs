using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Department");
        builder.HasKey(department => department.DepartmentId);
        builder.Property(department => department.DepartmentName).HasMaxLength(100).IsRequired();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role");
        builder.HasKey(role => role.RoleId);
        builder.Property(role => role.RoleName).HasMaxLength(100).IsRequired();
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Project");
        builder.HasKey(project => project.ProjectId);
        builder.Property(project => project.ProjectName).HasMaxLength(150).IsRequired();
        builder.Property(project => project.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(project => project.Status).HasDatabaseName("IX_Project_Status");
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee");
        builder.HasKey(employee => employee.EmployeeId);
        builder.Property(employee => employee.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(employee => employee.LastName).HasMaxLength(50).IsRequired();
        builder.Property(employee => employee.BaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(employee => employee.Bonus).HasColumnType("decimal(18,2)");
        builder.Property(employee => employee.NetPay).HasColumnType("decimal(18,2)");
        builder.Ignore(employee => employee.FullName);
        builder.HasIndex(employee => new { employee.DepartmentId, employee.HireDate })
            .HasDatabaseName("IX_Employee_DepartmentId_HireDate");

        builder.HasOne(employee => employee.Department)
            .WithMany(department => department.Employees)
            .HasForeignKey(employee => employee.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(employee => employee.Role)
            .WithMany(role => role.Employees)
            .HasForeignKey(employee => employee.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeProjectConfiguration : IEntityTypeConfiguration<EmployeeProject>
{
    public void Configure(EntityTypeBuilder<EmployeeProject> builder)
    {
        builder.ToTable("EmployeeProject");
        builder.HasKey(assignment => assignment.EmployeeProjectId);
        builder.Property(assignment => assignment.HoursWorked).HasColumnType("decimal(10,2)");
        builder.HasIndex(assignment => new { assignment.EmployeeId, assignment.ProjectId })
            .HasDatabaseName("IX_EmployeeProject_EmployeeId_ProjectId");

        builder.HasOne(assignment => assignment.Employee)
            .WithMany(employee => employee.EmployeeProjects)
            .HasForeignKey(assignment => assignment.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Project)
            .WithMany(project => project.EmployeeProjects)
            .HasForeignKey(assignment => assignment.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Role)
            .WithMany(role => role.EmployeeProjects)
            .HasForeignKey(assignment => assignment.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendance");
        builder.HasKey(attendance => attendance.AttendanceId);
        builder.HasIndex(attendance => new { attendance.EmployeeId, attendance.AttendanceDate })
            .HasDatabaseName("IX_Attendance_EmployeeId_AttendanceDate");

        builder.HasOne(attendance => attendance.Employee)
            .WithMany(employee => employee.Attendances)
            .HasForeignKey(attendance => attendance.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeDepartmentRoleViewConfiguration : IEntityTypeConfiguration<EmployeeDepartmentRoleView>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentRoleView> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_EmployeeDepartmentRole");
        builder.Property(row => row.FirstName).HasMaxLength(50);
        builder.Property(row => row.LastName).HasMaxLength(50);
        builder.Property(row => row.DepartmentName).HasMaxLength(100);
        builder.Property(row => row.RoleName).HasMaxLength(100);
    }
}
