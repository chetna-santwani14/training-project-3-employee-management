using EmployeeManagement.Filters;
using EmployeeManagement.Models;
using EmployeeManagement.Validators;

namespace EmployeeManagement.Tests;

public class FilterAndValidatorTests
{
    [Fact]
    public void ExpressionFilter_IgnoresBlankCriteria_AndCombinesTheRest()
    {
        var employee = new Employee
        {
            DepartmentId = 2,
            RoleId = 4,
            EmployeeProjects =
            [
                new EmployeeProject { Project = new Project { Status = ProjectStatuses.Completed } }
            ]
        };

        Assert.True(EmployeeExpressionFilter.Build(null, null, null).Compile()(employee));
        Assert.True(EmployeeExpressionFilter.Build(2, 4, ProjectStatuses.Completed).Compile()(employee));
        Assert.False(EmployeeExpressionFilter.Build(2, 4, ProjectStatuses.Active).Compile()(employee));
    }

    [Fact]
    public void PredicateFilter_TreatsTheHireDateRangeAsInclusive()
    {
        var employee = new Employee
        {
            HireDate = new DateOnly(2020, 1, 15),
            Department = new Department { DepartmentName = "Engineering" },
            Role = new Role { RoleName = "Developer" }
        };

        var sameDay = EmployeePredicateFilter.Build(null, null, employee.HireDate, employee.HireDate);
        var outside = EmployeePredicateFilter.Build("Sales", null, null, null);

        Assert.True(sameDay.Compile()(employee));
        Assert.False(outside.Compile()(employee));
        Assert.True(EmployeePredicateFilter.Build(null, null, null, null).Compile()(employee));
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("abc", false)]
    public void PageNumber_MustBeAtLeastOne(string input, bool expected)
        => Assert.Equal(expected, InputValidator.TryParsePage(input, out _));

    [Fact]
    public void GroupingKeys_DefaultToDepartmentAndRole()
    {
        Assert.True(InputValidator.TryParseGroupingKeys("  ", out var keys));
        Assert.Equal(["department", "role"], keys);
        Assert.False(InputValidator.TryParseGroupingKeys("salary", out _));
    }

    [Fact]
    public void DateRange_RejectsAnInvertedRange()
        => Assert.False(InputValidator.TryParseDateRange(new DateOnly(2024, 2, 1), new DateOnly(2024, 1, 1)));
}
