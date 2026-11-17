using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceRatingAndIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerformanceRating",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Employee",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.Sql("""
                ALTER TABLE [Employee] ADD CONSTRAINT [CK_Employee_PerformanceRating]
                CHECK ([PerformanceRating] IS NULL OR ([PerformanceRating] >= 1 AND [PerformanceRating] <= 5));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentId_HireDate",
                table: "Employee",
                columns: new[] { "DepartmentId", "HireDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProject_EmployeeId_ProjectId",
                table: "EmployeeProject",
                columns: new[] { "EmployeeId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmployeeId_AttendanceDate",
                table: "Attendance",
                columns: new[] { "EmployeeId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Project_Status",
                table: "Project",
                column: "Status");

            migrationBuilder.Sql(CreateViewSql);
            migrationBuilder.Sql(CreateProcedureSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.GetEmployeesByDepartment', N'P') IS NOT NULL
                    DROP PROCEDURE dbo.GetEmployeesByDepartment;
                """);
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.vw_EmployeeDepartmentRole', N'V') IS NOT NULL
                    DROP VIEW dbo.vw_EmployeeDepartmentRole;
                """);

            migrationBuilder.DropIndex(name: "IX_Project_Status", table: "Project");
            migrationBuilder.DropIndex(name: "IX_Attendance_EmployeeId_AttendanceDate", table: "Attendance");
            migrationBuilder.DropIndex(name: "IX_EmployeeProject_EmployeeId_ProjectId", table: "EmployeeProject");
            migrationBuilder.DropIndex(name: "IX_Employee_DepartmentId_HireDate", table: "Employee");

            migrationBuilder.Sql("""
                ALTER TABLE [Employee] DROP CONSTRAINT [CK_Employee_PerformanceRating];
                """);

            migrationBuilder.DropColumn(name: "RowVersion", table: "Employee");
            migrationBuilder.DropColumn(name: "PerformanceRating", table: "Employee");
        }

        private const string CreateViewSql = """
            CREATE OR ALTER VIEW dbo.vw_EmployeeDepartmentRole AS
            SELECT
                e.EmployeeId,
                e.FirstName,
                e.LastName,
                d.DepartmentName,
                r.RoleName
            FROM dbo.Employee AS e
            INNER JOIN dbo.Department AS d ON d.DepartmentId = e.DepartmentId
            LEFT JOIN dbo.Role AS r ON r.RoleId = e.RoleId
            WHERE NOT EXISTS (
                SELECT 1
                FROM dbo.EmployeeProject AS ep
                INNER JOIN dbo.Project AS p ON p.ProjectId = ep.ProjectId
                WHERE ep.EmployeeId = e.EmployeeId
                  AND p.Status = N'Maintenance'
            );
            """;

        private const string CreateProcedureSql = """
            CREATE OR ALTER PROCEDURE dbo.GetEmployeesByDepartment
                @DepartmentId INT = NULL,
                @RoleId INT = NULL,
                @SortBy NVARCHAR(30) = N'HireDate',
                @SortDirection NVARCHAR(4) = N'ASC'
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @SortBy NOT IN (N'FirstName', N'LastName', N'HireDate', N'DepartmentName')
                    SET @SortBy = N'HireDate';

                IF @SortDirection NOT IN (N'ASC', N'DESC')
                    SET @SortDirection = N'ASC';

                SELECT
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    d.DepartmentName,
                    r.RoleName,
                    e.HireDate,
                    DATEDIFF(YEAR, e.HireDate, GETDATE())
                        - CASE
                            WHEN DATEADD(YEAR, DATEDIFF(YEAR, e.HireDate, GETDATE()), e.HireDate) > CAST(GETDATE() AS date)
                            THEN 1 ELSE 0
                          END AS TenureYears,
                    (
                        SELECT COUNT(DISTINCT ep.ProjectId)
                        FROM dbo.EmployeeProject AS ep
                        WHERE ep.EmployeeId = e.EmployeeId
                    ) AS ProjectCount,
                    COUNT(*) OVER () AS TotalRows
                FROM dbo.Employee AS e
                INNER JOIN dbo.Department AS d ON d.DepartmentId = e.DepartmentId
                LEFT JOIN dbo.Role AS r ON r.RoleId = e.RoleId
                WHERE (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                  AND (@RoleId IS NULL OR e.RoleId = @RoleId)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.EmployeeProject AS ep
                      INNER JOIN dbo.Project AS p ON p.ProjectId = ep.ProjectId
                      WHERE ep.EmployeeId = e.EmployeeId
                        AND p.Status = N'Maintenance'
                  )
                ORDER BY
                    CASE WHEN @SortBy = N'FirstName' AND @SortDirection = N'ASC' THEN e.FirstName END ASC,
                    CASE WHEN @SortBy = N'FirstName' AND @SortDirection = N'DESC' THEN e.FirstName END DESC,
                    CASE WHEN @SortBy = N'LastName' AND @SortDirection = N'ASC' THEN e.LastName END ASC,
                    CASE WHEN @SortBy = N'LastName' AND @SortDirection = N'DESC' THEN e.LastName END DESC,
                    CASE WHEN @SortBy = N'HireDate' AND @SortDirection = N'ASC' THEN CONVERT(varchar(10), e.HireDate, 23) END ASC,
                    CASE WHEN @SortBy = N'HireDate' AND @SortDirection = N'DESC' THEN CONVERT(varchar(10), e.HireDate, 23) END DESC,
                    CASE WHEN @SortBy = N'DepartmentName' AND @SortDirection = N'ASC' THEN d.DepartmentName END ASC,
                    CASE WHEN @SortBy = N'DepartmentName' AND @SortDirection = N'DESC' THEN d.DepartmentName END DESC,
                    e.EmployeeId ASC;
            END
            """;
    }
}
