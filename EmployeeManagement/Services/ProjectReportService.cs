using EmployeeManagement.Data;
using EmployeeManagement.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services;

public class ProjectReportService
{
    private readonly IDbContextFactory<EmployeeDbContext> _factory;

    public ProjectReportService(IDbContextFactory<EmployeeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<ProjectStaffingDto>> GetProjectStaffingAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var projects = await context.Projects
            .AsNoTracking()
            .Include(project => project.EmployeeProjects)
                .ThenInclude(assignment => assignment.Employee)
            .Include(project => project.EmployeeProjects)
                .ThenInclude(assignment => assignment.Role)
            .OrderBy(project => project.ProjectName)
            .ToListAsync(cancellationToken);

        var rows = new List<ProjectStaffingDto>();
        foreach (var project in projects)
        {
            var assigned = project.EmployeeProjects
                .Where(assignment => assignment.Employee is not null)
                .OrderBy(assignment => assignment.Employee!.LastName)
                .ThenBy(assignment => assignment.Employee!.FirstName)
                .ToList();

            if (assigned.Count == 0)
            {
                rows.Add(new ProjectStaffingDto { ProjectName = project.ProjectName });
                continue;
            }

            rows.AddRange(assigned.Select(assignment => new ProjectStaffingDto
            {
                ProjectName = project.ProjectName,
                EmployeeName = $"{assignment.Employee!.FirstName} {assignment.Employee.LastName}",
                RoleName = assignment.Role.RoleName
            }));
        }

        return rows;
    }

    public async Task<ProjectDirectoryDto> GetProjectsPerEmployeeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var employees = await context.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new EmployeeProjectsDto
            {
                EmployeeName = employee.FirstName + " " + employee.LastName,
                Projects = employee.EmployeeProjects
                    .Select(assignment => assignment.Project.ProjectName)
                    .OrderBy(name => name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var uniqueNames = await context.Employees
            .AsNoTracking()
            .SelectMany(employee => employee.EmployeeProjects.Select(assignment => assignment.Project.ProjectName))
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return new ProjectDirectoryDto
        {
            Employees = employees,
            UniqueProjectNames = uniqueNames
        };
    }
}
