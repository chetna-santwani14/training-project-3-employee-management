using EmployeeManagement.Models;

namespace EmployeeManagement.Validators;

public static class InputValidator
{
    public static readonly string[] OrderFields = ["FirstName", "LastName", "HireDate"];
    public static readonly string[] GroupingKeys = ["department", "role"];
    public static readonly string[] SortFields = ["FirstName", "LastName", "HireDate", "DepartmentName"];

    public static bool TryParseMenuOption(string? input, out int option)
        => int.TryParse(input, out option) && option is >= 0 and <= 19;

    public static bool TryParsePositiveInt(string? input, out int value)
        => int.TryParse(input, out value) && value > 0;

    public static bool TryParseOptionalPositiveInt(string? input, out int? value)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            value = null;
            return true;
        }

        if (int.TryParse(input, out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryParsePage(string? input, out int page)
        => int.TryParse(input, out page) && page >= 1;

    public static bool TryParseOrderField(string? input, out string field)
    {
        field = OrderFields.FirstOrDefault(candidate =>
            candidate.Equals(input?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return field.Length > 0;
    }

    public static bool TryParseGroupingKeys(string? input, out string[] keys)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            keys = ["department", "role"];
            return true;
        }

        var parsed = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(key => key.ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (parsed.Length == 0 || parsed.Any(key => !GroupingKeys.Contains(key)))
        {
            keys = [];
            return false;
        }

        keys = parsed;
        return true;
    }

    public static bool TryParseDate(string? input, out DateOnly date)
        => DateOnly.TryParse(input, out date);

    public static bool TryParseOptionalDate(string? input, out DateOnly? date)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            date = null;
            return true;
        }

        if (DateOnly.TryParse(input, out var parsed))
        {
            date = parsed;
            return true;
        }

        date = null;
        return false;
    }

    public static bool TryParseTime(string? input, out TimeOnly time)
        => TimeOnly.TryParse(input, out time);

    public static bool TryParseDateRange(DateOnly? from, DateOnly? to)
        => from is null || to is null || from.Value <= to.Value;

    public static bool TryParseSalary(string? input, out decimal salary)
        => decimal.TryParse(input, out salary) && salary > 0;

    public static bool TryParseRating(string? input, out int rating)
        => int.TryParse(input, out rating) && rating is >= 1 and <= 5;

    public static bool TryParseProjectStatus(string? input, out string? status)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            status = null;
            return true;
        }

        status = ProjectStatuses.Known.FirstOrDefault(known =>
            known.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        return status is not null;
    }

    public static bool TryParseSort(string? sortByInput, string? directionInput, out string sortBy, out string direction)
    {
        if (string.IsNullOrWhiteSpace(sortByInput))
        {
            sortBy = "HireDate";
        }
        else
        {
            sortBy = SortFields.FirstOrDefault(field =>
                field.Equals(sortByInput.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            if (sortBy.Length == 0)
            {
                direction = string.Empty;
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(directionInput))
        {
            direction = "ASC";
            return true;
        }

        direction = directionInput.Trim().ToUpperInvariant();
        return direction is "ASC" or "DESC";
    }
}
