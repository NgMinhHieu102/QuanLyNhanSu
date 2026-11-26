using HumaHr.Models;
using Microsoft.EntityFrameworkCore;

namespace HumaHr.Data;

public class HumaHrDbContext(DbContextOptions<HumaHrDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentPosition> DepartmentPositions => Set<DepartmentPosition>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ProfileChangeRequest> ProfileChangeRequests => Set<ProfileChangeRequest>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<PayrollSettings> PayrollSettings => Set<PayrollSettings>();
    public DbSet<CompanyHoliday> CompanyHolidays => Set<CompanyHoliday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicit Unicode lengths keep SQL Server indexes bounded and preserve Vietnamese text.
        modelBuilder.Entity<AppUser>().Property(x => x.Email).HasMaxLength(160);
        modelBuilder.Entity<AppUser>().Property(x => x.FullName).HasMaxLength(120);
        modelBuilder.Entity<AppUser>().Property(x => x.Role).HasMaxLength(32);
        modelBuilder.Entity<AppUser>().HasIndex(x => x.EmployeeId).IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL");
        modelBuilder.Entity<Attendance>().Property(x => x.EmployeeName).HasMaxLength(120);
        modelBuilder.Entity<Attendance>().Property(x => x.Status).HasMaxLength(32);
        modelBuilder.Entity<LeaveRequest>().Property(x => x.EmployeeName).HasMaxLength(120);
        modelBuilder.Entity<LeaveRequest>().Property(x => x.LeaveType).HasMaxLength(60);
        modelBuilder.Entity<LeaveRequest>().Property(x => x.Status).HasMaxLength(32);
        modelBuilder.Entity<LeaveRequest>().Property(x => x.AdminNote).HasMaxLength(500);
        modelBuilder.Entity<ProfileChangeRequest>().Property(x => x.EmployeeName).HasMaxLength(120);
        modelBuilder.Entity<ProfileChangeRequest>().Property(x => x.ProposedEmail).HasMaxLength(160);
        modelBuilder.Entity<ProfileChangeRequest>().Property(x => x.ProposedPhone).HasMaxLength(30);
        modelBuilder.Entity<ProfileChangeRequest>().Property(x => x.Status).HasMaxLength(32);
        modelBuilder.Entity<PasswordResetRequest>().Property(x => x.EmployeeName).HasMaxLength(120);
        modelBuilder.Entity<PasswordResetRequest>().Property(x => x.Email).HasMaxLength(160);
        modelBuilder.Entity<PasswordResetRequest>().Property(x => x.Status).HasMaxLength(32);
        modelBuilder.Entity<Payroll>().Property(x => x.EmployeeName).HasMaxLength(120);
        modelBuilder.Entity<Payroll>().Property(x => x.Department).HasMaxLength(80);
        modelBuilder.Entity<Payroll>().Property(x => x.Period).HasMaxLength(7);
        modelBuilder.Entity<Payroll>().Property(x => x.Status).HasMaxLength(40);
        modelBuilder.Entity<CompanyHoliday>().Property(x => x.Name).HasMaxLength(160);
        modelBuilder.Entity<CompanyHoliday>().Property(x => x.Date).HasColumnType("date");
        modelBuilder.Entity<PayrollSettings>().HasData(new PayrollSettings { Id = 1 });
        modelBuilder.Entity<Employee>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Employee>().Ignore(x => x.CreateAccount);
        modelBuilder.Entity<Employee>().Ignore(x => x.AccountStatus);
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Employee>().Property(x => x.Salary).HasPrecision(18, 2);
        modelBuilder.Entity<Attendance>().Property(x => x.OvertimeHours).HasPrecision(5, 2);
        modelBuilder.Entity<Attendance>().Property(x => x.NightHours).HasPrecision(5, 2);
        modelBuilder.Entity<Attendance>().Property(x => x.NightOvertimeHours).HasPrecision(5, 2);
        modelBuilder.Entity<LeaveRequest>().Property(x => x.Reason).HasMaxLength(500);
        modelBuilder.Entity<ProfileChangeRequest>().Property(x => x.Reason).HasMaxLength(300);
        modelBuilder.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Department>().Ignore(x => x.Headcount);
        modelBuilder.Entity<DepartmentPosition>().HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();
        modelBuilder.Entity<DepartmentPosition>().HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Payroll>().Property(x => x.BaseSalary).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.Allowance).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.Deduction).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.PaidDays).HasPrecision(6, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.UnpaidDays).HasPrecision(6, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.BasePay).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.HoursPerDayUsed).HasPrecision(5, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.HourlyRateUsed).HasPrecision(18, 4);
        modelBuilder.Entity<Payroll>().Property(x => x.RegularOvertimeRateUsed).HasPrecision(6, 3);
        modelBuilder.Entity<Payroll>().Property(x => x.RestOvertimeRateUsed).HasPrecision(6, 3);
        modelBuilder.Entity<Payroll>().Property(x => x.HolidayOvertimeRateUsed).HasPrecision(6, 3);
        modelBuilder.Entity<Payroll>().Property(x => x.NightPremiumRateUsed).HasPrecision(6, 4);
        modelBuilder.Entity<Payroll>().Property(x => x.NightOvertimeAdditionalRateUsed).HasPrecision(6, 4);
        modelBuilder.Entity<Payroll>().Property(x => x.EmployeeInsuranceRateUsed).HasPrecision(6, 4);
        modelBuilder.Entity<Payroll>().Property(x => x.EmployerContributionRateUsed).HasPrecision(6, 4);
        modelBuilder.Entity<Payroll>().Property(x => x.RegularOvertimeHours).HasPrecision(7, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.RestOvertimeHours).HasPrecision(7, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.HolidayOvertimeHours).HasPrecision(7, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.NightHours).HasPrecision(7, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.RegularOvertimePay).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.RestOvertimePay).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.HolidayOvertimePay).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.NightPremium).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.NightOvertimeHours).HasPrecision(7, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.NightOvertimePremium).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.Bonus).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.InsuranceBase).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.EmployeeInsurance).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.EmployerContributions).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.PersonalIncomeTax).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.AdvanceDeduction).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.OtherDeductions).HasPrecision(18, 2);
        modelBuilder.Entity<Payroll>().Property(x => x.OtherDeductionReason).HasMaxLength(300);
        modelBuilder.Entity<Payroll>().Property(x => x.AdjustmentReason).HasMaxLength(300);
        modelBuilder.Entity<Payroll>().Ignore(x => x.NetSalary);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.HoursPerDay).HasPrecision(5, 2);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.EmployeeInsuranceRate).HasPrecision(6, 4);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.EmployerContributionRate).HasPrecision(6, 4);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.RegularOvertimeRate).HasPrecision(6, 3);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.RestOvertimeRate).HasPrecision(6, 3);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.HolidayOvertimeRate).HasPrecision(6, 3);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.NightPremiumRate).HasPrecision(6, 4);
        modelBuilder.Entity<PayrollSettings>().Property(x => x.NightOvertimeAdditionalRate).HasPrecision(6, 4);
        modelBuilder.Entity<CompanyHoliday>().HasIndex(x => x.Date).IsUnique();
    }
}
