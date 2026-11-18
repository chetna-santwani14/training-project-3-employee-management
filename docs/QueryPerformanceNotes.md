# Query performance notes

Notes for the review. Reads use `AsNoTracking`. The query stays `IQueryable` until one `ToListAsync` (or `Count` / `Any` / `All` / `Max`).

| Technique | Where it is used |
| --- | --- |
| Eager loading in one SQL statement | Project staffing loads `Project → EmployeeProject → Employee` and `Role` with `Include` / `ThenInclude` (FR-001). |
| Projection instead of full entities | Highest hours, payroll groups, role aggregates, paging, and filters select only the columns the screen shows (FR-003, FR-006, FR-007, FR-010, FR-012, FR-013). |
| Server-side aggregation | `Sum`, `Average`, `Count`, `Any`, and `All` run in the database (FR-003, FR-006, FR-008, FR-010). |
| Percentile finished in memory | Salary bands load id, name, and salary, then rank in memory, because the percentile definition does not translate cleanly (FR-005). |
| Stable paging | Page size is 10. `OrderBy` the chosen field, then `ThenBy(EmployeeId)`, then `Skip` / `Take` (FR-007). |
| Whitelisted dynamic order and group | Order fields and grouping keys are chosen from a fixed list, so user input never becomes SQL text (FR-006, FR-007). |
| Expression filters on `IQueryable` | Expression-tree and Predicate Builder filters are passed to `Where` before execution, so SQL does the filtering (FR-012, FR-013). |
| Parameterized raw SQL | Status and the NetPay threshold are interpolated through EF, which sends parameters (FR-014). |
| Stored procedure parameters | `GetEmployeesByDepartment` receives `SqlParameter` values. Sort column and direction are checked against a fixed list inside the procedure (FR-019). |
| Indexes | `Employee (DepartmentId, HireDate)`, `EmployeeProject (EmployeeId, ProjectId)`, `Attendance (EmployeeId, AttendanceDate)`, `Project (Status)` (FR-004, FR-008, FR-012, FR-016). |
| Global query filter | Employees on a Maintenance project are removed in the model, so every normal LINQ query inherits the rule (FR-016). Raw SQL and the view repeat the same predicate because they bypass the filter. |
| Transaction | Base salary and attendance commit or roll back together (FR-015). |
| Concurrency token | `RowVersion` is a SQL Server `rowversion`. The second save fails with `DbUpdateConcurrencyException` (FR-017). |

Query syntax is used for hire order (FR-004), role aggregates (FR-010), and the left-join assignment list (FR-011). The other reports use method syntax.

`ToQueryString()` on the hire-order and role-aggregate queries is the quick way to confirm EF sent one statement rather than a row-by-row loop.
