using System.Linq.Expressions;
using EmployeeManagement.Models;

namespace EmployeeManagement.Filters;

public static class EmployeeExpressionFilter
{
    public static Expression<Func<Employee, bool>> Build(int? departmentId, int? roleId, string? projectStatus)
    {
        var employee = Expression.Parameter(typeof(Employee), "e");
        Expression body = Expression.Constant(true);

        if (departmentId is not null)
        {
            body = Expression.AndAlso(
                body,
                Expression.Equal(
                    Expression.Property(employee, nameof(Employee.DepartmentId)),
                    Expression.Constant(departmentId.Value)));
        }

        if (roleId is not null)
        {
            body = Expression.AndAlso(
                body,
                Expression.Equal(
                    Expression.Property(employee, nameof(Employee.RoleId)),
                    Expression.Constant(roleId, typeof(int?))));
        }

        if (!string.IsNullOrWhiteSpace(projectStatus))
        {
            body = Expression.AndAlso(body, ProjectStatusEquals(employee, projectStatus));
        }

        return Expression.Lambda<Func<Employee, bool>>(body, employee);
    }

    private static Expression ProjectStatusEquals(ParameterExpression employee, string projectStatus)
    {
        var assignment = Expression.Parameter(typeof(EmployeeProject), "ep");
        var status = Expression.Property(
            Expression.Property(assignment, nameof(EmployeeProject.Project)),
            nameof(Project.Status));
        var match = Expression.Lambda<Func<EmployeeProject, bool>>(
            Expression.Equal(status, Expression.Constant(projectStatus)),
            assignment);

        var any = typeof(Enumerable)
            .GetMethods()
            .Single(method => method.Name == nameof(Enumerable.Any) && method.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(EmployeeProject));

        return Expression.Call(
            any,
            Expression.Property(employee, nameof(Employee.EmployeeProjects)),
            match);
    }
}
