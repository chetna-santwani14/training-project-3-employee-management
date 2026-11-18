using System.Linq.Expressions;
using EmployeeManagement.Models;

namespace EmployeeManagement.Filters;

public static class EmployeePredicateFilter
{
    public static Expression<Func<Employee, bool>> Build(
        string? departmentName,
        string? roleName,
        DateOnly? hireFrom,
        DateOnly? hireTo)
    {
        var predicate = PredicateBuilder.New<Employee>(true);

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            predicate = predicate.And(employee => employee.Department.DepartmentName == departmentName);
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            predicate = predicate.And(employee => employee.Role != null && employee.Role.RoleName == roleName);
        }

        if (hireFrom is not null)
        {
            // Copy the date out. A nullable captured in the lambda does not translate.
            var from = hireFrom.Value;
            predicate = predicate.And(employee => employee.HireDate >= from);
        }

        if (hireTo is not null)
        {
            var to = hireTo.Value;
            predicate = predicate.And(employee => employee.HireDate <= to);
        }

        return predicate;
    }
}
