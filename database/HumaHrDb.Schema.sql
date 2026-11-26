-- Generated from the SQL Server EF Core model.
-- Optional: run only inside an EMPTY HumaHrDb database.
-- Normally start the application instead: it creates the database and seeds demo data.
-- This script creates tables and settings; employees/admin are seeded by the app.

CREATE TABLE [AppUsers] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(160) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [Role] nvarchar(32) NOT NULL,
    [EmployeeId] int NULL,
    [MustChangePassword] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Attendances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [EmployeeName] nvarchar(120) NOT NULL,
    [Date] datetime2 NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [IsLate] bit NOT NULL,
    [LeftEarly] bit NOT NULL,
    [OvertimeHours] decimal(5,2) NOT NULL,
    [CheckInAt] datetime2 NULL,
    [CheckOutAt] datetime2 NULL,
    [NightHours] decimal(5,2) NOT NULL,
    [NightOvertimeHours] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CompanyHolidays] (
    [Id] int NOT NULL IDENTITY,
    [Date] date NOT NULL,
    [Name] nvarchar(160) NOT NULL,
    CONSTRAINT [PK_CompanyHolidays] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Manager] nvarchar(120) NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [Email] nvarchar(160) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [Department] nvarchar(80) NOT NULL,
    [Position] nvarchar(80) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [Salary] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [EmployeeName] nvarchar(120) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [LeaveType] nvarchar(60) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [AdminNote] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PasswordResetRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [EmployeeName] nvarchar(120) NOT NULL,
    [Email] nvarchar(160) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PasswordResetRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Payrolls] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [EmployeeName] nvarchar(120) NOT NULL,
    [Department] nvarchar(80) NOT NULL,
    [Period] nvarchar(7) NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [Allowance] decimal(18,2) NOT NULL,
    [Deduction] decimal(18,2) NOT NULL,
    [IsCalculated] bit NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [ScheduledWorkDays] int NOT NULL,
    [PaidDays] decimal(6,2) NOT NULL,
    [UnpaidDays] decimal(6,2) NOT NULL,
    [BasePay] decimal(18,2) NOT NULL,
    [HoursPerDayUsed] decimal(5,2) NOT NULL,
    [HourlyRateUsed] decimal(18,4) NOT NULL,
    [RegularOvertimeRateUsed] decimal(6,3) NOT NULL,
    [RestOvertimeRateUsed] decimal(6,3) NOT NULL,
    [HolidayOvertimeRateUsed] decimal(6,3) NOT NULL,
    [NightPremiumRateUsed] decimal(6,4) NOT NULL,
    [NightOvertimeAdditionalRateUsed] decimal(6,4) NOT NULL,
    [EmployeeInsuranceRateUsed] decimal(6,4) NOT NULL,
    [EmployerContributionRateUsed] decimal(6,4) NOT NULL,
    [RegularOvertimeHours] decimal(7,2) NOT NULL,
    [RestOvertimeHours] decimal(7,2) NOT NULL,
    [HolidayOvertimeHours] decimal(7,2) NOT NULL,
    [NightHours] decimal(7,2) NOT NULL,
    [NightOvertimeHours] decimal(7,2) NOT NULL,
    [RegularOvertimePay] decimal(18,2) NOT NULL,
    [RestOvertimePay] decimal(18,2) NOT NULL,
    [HolidayOvertimePay] decimal(18,2) NOT NULL,
    [NightPremium] decimal(18,2) NOT NULL,
    [NightOvertimePremium] decimal(18,2) NOT NULL,
    [Bonus] decimal(18,2) NOT NULL,
    [InsuranceBase] decimal(18,2) NOT NULL,
    [EmployeeInsurance] decimal(18,2) NOT NULL,
    [EmployerContributions] decimal(18,2) NOT NULL,
    [PersonalIncomeTax] decimal(18,2) NOT NULL,
    [AdvanceDeduction] decimal(18,2) NOT NULL,
    [OtherDeductions] decimal(18,2) NOT NULL,
    [OtherDeductionReason] nvarchar(300) NOT NULL,
    [AdjustmentReason] nvarchar(300) NOT NULL,
    [AdjustmentCount] int NOT NULL,
    [PublishedAt] datetime2 NULL,
    CONSTRAINT [PK_Payrolls] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PayrollSettings] (
    [Id] int NOT NULL IDENTITY,
    [WorkdaysMask] int NOT NULL,
    [HoursPerDay] decimal(5,2) NOT NULL,
    [WorkdayStart] time NOT NULL,
    [WorkdayEnd] time NOT NULL,
    [EmployeeInsuranceRate] decimal(6,4) NOT NULL,
    [EmployerContributionRate] decimal(6,4) NOT NULL,
    [RegularOvertimeRate] decimal(6,3) NOT NULL,
    [RestOvertimeRate] decimal(6,3) NOT NULL,
    [HolidayOvertimeRate] decimal(6,3) NOT NULL,
    [NightPremiumRate] decimal(6,4) NOT NULL,
    [NightOvertimeAdditionalRate] decimal(6,4) NOT NULL,
    CONSTRAINT [PK_PayrollSettings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ProfileChangeRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [EmployeeName] nvarchar(120) NOT NULL,
    [ProposedEmail] nvarchar(160) NOT NULL,
    [ProposedPhone] nvarchar(30) NOT NULL,
    [Reason] nvarchar(300) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProfileChangeRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [DepartmentPositions] (
    [Id] int NOT NULL IDENTITY,
    [DepartmentId] int NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    CONSTRAINT [PK_DepartmentPositions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DepartmentPositions_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EmployeeInsuranceRate', N'EmployerContributionRate', N'HolidayOvertimeRate', N'HoursPerDay', N'NightOvertimeAdditionalRate', N'NightPremiumRate', N'RegularOvertimeRate', N'RestOvertimeRate', N'WorkdayEnd', N'WorkdayStart', N'WorkdaysMask') AND [object_id] = OBJECT_ID(N'[PayrollSettings]'))
    SET IDENTITY_INSERT [PayrollSettings] ON;
INSERT INTO [PayrollSettings] ([Id], [EmployeeInsuranceRate], [EmployerContributionRate], [HolidayOvertimeRate], [HoursPerDay], [NightOvertimeAdditionalRate], [NightPremiumRate], [RegularOvertimeRate], [RestOvertimeRate], [WorkdayEnd], [WorkdayStart], [WorkdaysMask])
VALUES (1, 0.105, 0.215, 3.0, 8.0, 0.2, 0.3, 1.5, 2.0, '18:00:00', '09:00:00', 31);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EmployeeInsuranceRate', N'EmployerContributionRate', N'HolidayOvertimeRate', N'HoursPerDay', N'NightOvertimeAdditionalRate', N'NightPremiumRate', N'RegularOvertimeRate', N'RestOvertimeRate', N'WorkdayEnd', N'WorkdayStart', N'WorkdaysMask') AND [object_id] = OBJECT_ID(N'[PayrollSettings]'))
    SET IDENTITY_INSERT [PayrollSettings] OFF;
GO


CREATE UNIQUE INDEX [IX_AppUsers_Email] ON [AppUsers] ([Email]);
GO


CREATE UNIQUE INDEX [IX_AppUsers_EmployeeId] ON [AppUsers] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_CompanyHolidays_Date] ON [CompanyHolidays] ([Date]);
GO


CREATE UNIQUE INDEX [IX_DepartmentPositions_DepartmentId_Name] ON [DepartmentPositions] ([DepartmentId], [Name]);
GO


CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
GO


CREATE UNIQUE INDEX [IX_Employees_Code] ON [Employees] ([Code]);
GO


