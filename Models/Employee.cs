using System.ComponentModel.DataAnnotations;

namespace HumaHr.Models;

public class Employee
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string Code { get; set; } = "";
    [Required, StringLength(120)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    public string AccountStatus { get; set; } = "Chưa cấp";
    [StringLength(30)] public string Phone { get; set; } = "";
    [StringLength(80)] public string Department { get; set; } = "";
    [StringLength(80)] public string Position { get; set; } = "";
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public bool CreateAccount { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Department
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [StringLength(120)] public string Manager { get; set; } = "";
    public int Headcount { get; set; }
}

public class DepartmentPosition
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
}

public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Có mặt";
    public bool IsLate { get; set; }
    public bool LeftEarly { get; set; }
    public decimal OvertimeHours { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public decimal NightHours { get; set; }
    public decimal NightOvertimeHours { get; set; }
}

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = "";
    public string LeaveType { get; set; } = "Nghỉ phép năm có lương";
    public string Status { get; set; } = "Chờ duyệt";
    public string AdminNote { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ProfileChangeRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string ProposedEmail { get; set; } = "";
    public string ProposedPhone { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Chờ duyệt";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class PasswordResetRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "Chờ xử lý";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Payroll
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Period { get; set; } = "";
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public bool IsCalculated { get; set; }
    public string Status { get; set; } = "Bản nháp";
    public int ScheduledWorkDays { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public decimal BasePay { get; set; }
    public decimal HoursPerDayUsed { get; set; }
    public decimal HourlyRateUsed { get; set; }
    public decimal RegularOvertimeRateUsed { get; set; }
    public decimal RestOvertimeRateUsed { get; set; }
    public decimal HolidayOvertimeRateUsed { get; set; }
    public decimal NightPremiumRateUsed { get; set; }
    public decimal NightOvertimeAdditionalRateUsed { get; set; }
    public decimal EmployeeInsuranceRateUsed { get; set; }
    public decimal EmployerContributionRateUsed { get; set; }
    public decimal RegularOvertimeHours { get; set; }
    public decimal RestOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public decimal NightHours { get; set; }
    public decimal NightOvertimeHours { get; set; }
    public decimal RegularOvertimePay { get; set; }
    public decimal RestOvertimePay { get; set; }
    public decimal HolidayOvertimePay { get; set; }
    public decimal NightPremium { get; set; }
    public decimal NightOvertimePremium { get; set; }
    public decimal Bonus { get; set; }
    public decimal InsuranceBase { get; set; }
    public decimal EmployeeInsurance { get; set; }
    public decimal EmployerContributions { get; set; }
    public decimal PersonalIncomeTax { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public string OtherDeductionReason { get; set; } = "";
    public string AdjustmentReason { get; set; } = "";
    public int AdjustmentCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public decimal NetSalary => IsCalculated
        ? BasePay + Allowance + Bonus + RegularOvertimePay + RestOvertimePay + HolidayOvertimePay + NightPremium + NightOvertimePremium - EmployeeInsurance - PersonalIncomeTax - AdvanceDeduction - OtherDeductions
        : BaseSalary + Allowance - Deduction;
}

public class PayrollSettings
{
    public int Id { get; set; }
    public int WorkdaysMask { get; set; } = 31; // Monday-Friday
    public decimal HoursPerDay { get; set; } = 8m;
    public TimeSpan WorkdayStart { get; set; } = new(9, 0, 0);
    public TimeSpan WorkdayEnd { get; set; } = new(18, 0, 0);
    public decimal EmployeeInsuranceRate { get; set; } = 0.105m;
    public decimal EmployerContributionRate { get; set; } = 0.215m;
    public decimal RegularOvertimeRate { get; set; } = 1.5m;
    public decimal RestOvertimeRate { get; set; } = 2m;
    public decimal HolidayOvertimeRate { get; set; } = 3m;
    public decimal NightPremiumRate { get; set; } = 0.3m;
    public decimal NightOvertimeAdditionalRate { get; set; } = 0.2m;
}

public class CompanyHoliday
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
}
