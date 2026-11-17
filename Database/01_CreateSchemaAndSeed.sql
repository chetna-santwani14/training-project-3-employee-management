/*
    Starter database for Training Project 3.
    This script creates the tables the brief treats as already present.
    PerformanceRating, RowVersion, indexes, the view and the stored procedure
    are applied afterwards by EF Core migrations.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID(N'dbo.EmployeeProject', N'U') IS NOT NULL DROP TABLE dbo.EmployeeProject;
IF OBJECT_ID(N'dbo.Employee', N'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID(N'dbo.Project', N'U') IS NOT NULL DROP TABLE dbo.Project;
IF OBJECT_ID(N'dbo.Role', N'U') IS NOT NULL DROP TABLE dbo.Role;
IF OBJECT_ID(N'dbo.Department', N'U') IS NOT NULL DROP TABLE dbo.Department;
GO

CREATE TABLE dbo.Department
(
    DepartmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
    DepartmentName NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Role
(
    RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Project
(
    ProjectId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Project PRIMARY KEY,
    ProjectName NVARCHAR(150) NOT NULL,
    Status NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.Employee
(
    EmployeeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    HireDate DATE NOT NULL,
    DepartmentId INT NOT NULL,
    RoleId INT NULL,
    BaseSalary DECIMAL(18,2) NULL,
    Bonus DECIMAL(18,2) NULL,
    NetPay DECIMAL(18,2) NULL,
    CONSTRAINT FK_Employee_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department (DepartmentId),
    CONSTRAINT FK_Employee_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role (RoleId)
);

CREATE TABLE dbo.EmployeeProject
(
    EmployeeProjectId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeProject PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ProjectId INT NOT NULL,
    RoleId INT NOT NULL,
    HoursWorked DECIMAL(10,2) NULL,
    CONSTRAINT FK_EmployeeProject_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (EmployeeId),
    CONSTRAINT FK_EmployeeProject_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (ProjectId),
    CONSTRAINT FK_EmployeeProject_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role (RoleId)
);

CREATE TABLE dbo.Attendance
(
    AttendanceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY,
    EmployeeId INT NOT NULL,
    AttendanceDate DATE NOT NULL,
    CheckInTime TIME NOT NULL,
    CONSTRAINT FK_Attendance_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (EmployeeId)
);
GO

SET IDENTITY_INSERT dbo.Department ON;
INSERT INTO dbo.Department (DepartmentId, DepartmentName) VALUES
    (1, N'Engineering'),
    (2, N'Sales'),
    (3, N'Human Resources'),
    (4, N'Finance'),
    (5, N'Operations');
SET IDENTITY_INSERT dbo.Department OFF;

SET IDENTITY_INSERT dbo.Role ON;
INSERT INTO dbo.Role (RoleId, RoleName) VALUES
    (1, N'Developer'),
    (2, N'Intern'),
    (3, N'Manager'),
    (4, N'Analyst'),
    (5, N'Sales Executive');
SET IDENTITY_INSERT dbo.Role OFF;

SET IDENTITY_INSERT dbo.Project ON;
INSERT INTO dbo.Project (ProjectId, ProjectName, Status) VALUES
    (1, N'Revamp Website', N'Active'),
    (2, N'Mobile App', N'Completed'),
    (3, N'Data Warehouse', N'Completed'),
    (4, N'Legacy Maintenance', N'Maintenance'),
    (5, N'Intranet Portal', N'Active'),
    (6, N'Payroll Upgrade', N'Completed'),
    (7, N'Empty Pilot', N'On Hold');
SET IDENTITY_INSERT dbo.Project OFF;

SET IDENTITY_INSERT dbo.Employee ON;
INSERT INTO dbo.Employee (EmployeeId, FirstName, LastName, HireDate, DepartmentId, RoleId, BaseSalary, Bonus, NetPay) VALUES
    (1,  N'Riya',   N'Patel',   '2018-01-10', 1, 1, 3000,  100,  2800),
    (2,  N'Arjun',  N'Mehta',   '2019-03-01', 1, 1, 3500,  200,  3200),
    (3,  N'Neha',   N'Gupta',   '2019-03-01', 1, 3, 4000,  NULL, 6100),
    (4,  N'Vikram', N'Singh',   '2020-06-15', 1, 1, 4500,  150,  4500),
    (5,  N'Ananya', N'Rao',     '2021-02-01', 1, 2, 5000,  0,    4800),
    (6,  N'Kabir',  N'Shah',    '2022-11-20', 1, 1, 6000,  300,  5500),
    (7,  N'Sara',   N'Khan',    '2017-05-01', 2, 5, 7000,  400,  6400),
    (8,  N'Rohan',  N'Das',     '2020-08-12', 2, 5, 8000,  250,  7600),
    (9,  N'Isha',   N'Verma',   '2023-04-04', 2, 2, 9000,  100,  8200),
    (10, N'Pooja',  N'Nair',    '2016-02-02', 3, 4, 12000, 500,  11000),
    (11, N'Amit',   N'Joshi',   '2018-07-19', 3, 4, 5500,  100,  5000),
    (12, N'Leela',  N'Menon',   '2021-09-01', 3, 3, 6200,  0,    5800),
    (13, N'Farhan', N'Ali',     '2024-01-15', 3, 4, NULL,  NULL, NULL),
    (14, N'Devika', N'Rao',     '2019-12-01', 4, 4, 4800,  80,   4300),
    (15, N'Suresh', N'Iyer',    '2023-06-01', 1, 1, 10000, 100,  9000),
    (16, N'Meera',  N'Kapoor',  '2024-02-02', 1, 1, 11000, 100,  10000);
SET IDENTITY_INSERT dbo.Employee OFF;

SET IDENTITY_INSERT dbo.EmployeeProject ON;
INSERT INTO dbo.EmployeeProject (EmployeeProjectId, EmployeeId, ProjectId, RoleId, HoursWorked) VALUES
    (1,  1, 1, 1, 40),
    (2,  1, 5, 1, 30),
    (3,  2, 2, 1, 70),
    (4,  3, 3, 3, 20),
    (5,  4, 6, 1, 15),
    (6,  4, 5, 1, NULL),
    (7,  5, 5, 2, 5),
    (8,  6, 2, 1, 25),
    (9,  6, 3, 1, 8),
    (10, 7, 1, 5, 12),
    (11, 8, 1, 5, 8),
    (12, 9, 5, 2, 4),
    (13, 10, 3, 4, 18),
    (14, 11, 6, 4, 6),
    (15, 12, 1, 3, 9),
    (16, 15, 4, 1, 50),
    (17, 16, 4, 1, 10),
    (18, 16, 1, 1, 6);
SET IDENTITY_INSERT dbo.EmployeeProject OFF;

INSERT INTO dbo.Attendance (EmployeeId, AttendanceDate, CheckInTime) VALUES
    (7, '2026-03-02', '08:15'),
    (8, '2026-03-02', '08:40'),
    (9, '2026-03-02', '08:55'),
    (7, '2026-03-03', '08:10'),
    (8, '2026-03-03', '09:30');
GO
