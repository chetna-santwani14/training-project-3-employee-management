# Training Project 3 — Employee Management System

Console application for the LINQ and EF Core training brief. It answers the 19 HR, payroll, and staffing questions from the BRD and FSD, and adds the cross-cutting quality rules (FR-020).

## Assumptions taken from the open questions

- Problems 7–14 are included. The success criteria ask for all 19 statements.
- `PerformanceRating` is a nullable integer. Allowed values are null or 1–5.
- An employee on at least one project with status `Maintenance` is hidden from standard queries. `IgnoreQueryFilters()` is the explicit exception (menu option 16).
- A salary percentile is the share of employees with a strictly lower salary. 30 maps to Mid and 70 maps to High. Equal salaries share a percentile. Missing salaries are listed separately.
- `Bonus` and `NetPay` are columns on `Employee`. `RoleId` exists on both `Employee` and `EmployeeProject`.

## Prerequisites

- .NET 8 SDK
- SQL Server Express LocalDB, instance `(localdb)\MSSQLLocalDB`

The automated tests do not need SQL Server. They run the same services against SQLite.

## Run

```bash
dotnet test
dotnet run --project EmployeeManagement
```

On startup the app creates `EmployeeManagementDb` if needed, loads `Database/01_CreateSchemaAndSeed.sql` when the `Employee` table is missing, and applies EF Core migrations. The migration adds `PerformanceRating`, `RowVersion`, the supporting indexes, `vw_EmployeeDepartmentRole`, and `GetEmployeesByDepartment`.

The LocalDB connection string is in `EmployeeManagement/appsettings.json`. For any shared server, replace it with user secrets:

```bash
dotnet user-secrets init --project EmployeeManagement
dotnet user-secrets set "ConnectionStrings:EmployeeManagement" "<connection string>" --project EmployeeManagement
```

## Layout

- `EmployeeManagement/Models` — table shapes. Custom columns stay in `Employee.Custom.cs`.
- `EmployeeManagement/Data` — `EmployeeDbContext`, Fluent configuration, startup bootstrap.
- `EmployeeManagement/Services` — one service per report area. The menu never receives entities.
- `EmployeeManagement/Filters` — expression-tree filter and Predicate Builder.
- `EmployeeManagement/Presentation` — numbered console menu.
- `Database/01_CreateSchemaAndSeed.sql` — starter schema and sample rows.
- `docs/QueryPerformanceNotes.md` — review notes for query shape and indexes.

## Menu

| Option | Requirement |
| --- | --- |
| 1 | Project staffing, eager loaded |
| 2 | PerformanceRating migration status |
| 3 | Highest total working hours, ties included |
| 4 | First, last, and 5th hired employee per department |
| 5 | Salary percentile bands |
| 6 | Dynamic department / role grouping |
| 7 | 10 employees per page, selectable order |
| 8 | Sales check-in, Intern exists, Revamp Website exists |
| 9 | Projects per employee and unique project names |
| 10 | Bonus, NetPay, and project count by role |
| 11 | Every employee, including people with no project |
| 12 | Expression-tree filter |
| 13 | Predicate Builder filter |
| 14 | Parameterized SQL for completed projects and NetPay above 5000 |
| 15 | Salary update and attendance insert in one transaction |
| 16 | Maintenance filter, with and without `IgnoreQueryFilters()` |
| 17 | Two contexts updating one employee |
| 18 | `vw_EmployeeDepartmentRole` |
| 19 | `GetEmployeesByDepartment` |
| 0 | Exit |

## Sample data worth knowing

- Riya Patel and Arjun Mehta are tied for the most hours (70). Suresh Iyer and Meera Kapoor are hidden because they are on Legacy Maintenance.
- Engineering has a 5th hire (Ananya Rao). Human Resources has four people, so the 5th column is N/A.
- 2026-03-02: every Sales employee checked in before 09:00. 2026-03-03: Rohan Das checked in at 09:30.
- Farhan Ali has no salary. Devika Rao and Farhan Ali have no project.
- Option 15 asks whether to fail after the salary change. Answering `y` rolls both changes back.

## Database First and Code First

The starter script is the existing database. Custom fields are added with Code First:

```bash
dotnet ef migrations add <Name> --project EmployeeManagement --startup-project EmployeeManagement
dotnet ef database update --project EmployeeManagement --startup-project EmployeeManagement
```

`InitialBaseline` has an empty `Up` so it can be applied to the database the script already created. `AddPerformanceRatingAndIntegrity` adds the new column, row version, indexes, view, and stored procedure. Reverting that migration removes them:

```bash
dotnet ef database update InitialBaseline --project EmployeeManagement --startup-project EmployeeManagement
```

Scaffold command, if you want to regenerate the table shapes from a live database:

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:EmployeeManagement" Microsoft.EntityFrameworkCore.SqlServer --project EmployeeManagement --output-dir Models --context EmployeeDbContext --context-dir Data --force
```

Keep `Employee.Custom.cs` and the Fluent configuration. A scaffold with `--force` overwrites the generated files.
