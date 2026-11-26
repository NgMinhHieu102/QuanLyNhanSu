using HumaHr.Data;
using HumaHr.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace HumaHr.Services;

public class HrRepository(HumaHrDbContext db)
{
    public int EmployeeCountAt(DateTime date) => db.Employees.AsNoTracking().Count(x => x.StartDate <= date);

    public HrData Snapshot() => new()
    {
        Employees = db.Employees.AsNoTracking().OrderBy(x => x.Id).ToList(),
        Departments = db.Departments.AsNoTracking().OrderBy(x => x.Id).ToList(),
        Attendances = db.Attendances.AsNoTracking().OrderBy(x => x.Id).ToList(),
        Payrolls = db.Payrolls.AsNoTracking().OrderBy(x => x.Id).ToList()
    };

    public List<DepartmentPosition> DepartmentPositions() => db.DepartmentPositions.AsNoTracking().OrderBy(x => x.DepartmentId).ThenBy(x => x.Name).ToList();

    public Dictionary<string, List<string>> PositionsByDepartment()
    {
        var departments = db.Departments.AsNoTracking().OrderBy(x => x.Name).ToList();
        var positions = db.DepartmentPositions.AsNoTracking().OrderBy(x => x.Name).ToList();
        return departments.ToDictionary(x => x.Name,
            x => positions.Where(p => p.DepartmentId == x.Id).Select(p => p.Name).ToList());
    }

    public bool PositionBelongsToDepartment(string departmentName, string positionName) =>
        db.DepartmentPositions.AsNoTracking().Any(p => p.Name == positionName && db.Departments.Any(d => d.Id == p.DepartmentId && d.Name == departmentName));

    public bool SaveDepartmentPosition(int departmentId, string name)
    {
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 80 || !db.Departments.Any(x => x.Id == departmentId) ||
            db.DepartmentPositions.Any(x => x.DepartmentId == departmentId && x.Name.ToLower() == name.ToLower())) return false;
        db.DepartmentPositions.Add(new DepartmentPosition { DepartmentId = departmentId, Name = name });
        db.SaveChanges();
        return true;
    }

    public bool DeleteDepartmentPosition(int id)
    {
        var position = db.DepartmentPositions.Find(id);
        if (position is null) return false;
        var departmentName = db.Departments.Where(x => x.Id == position.DepartmentId).Select(x => x.Name).FirstOrDefault();
        if (departmentName is null || db.Employees.Any(x => x.Department == departmentName && x.Position == position.Name)) return false;
        db.DepartmentPositions.Remove(position);
        db.SaveChanges();
        return true;
    }

    public bool SaveEmployee(Employee employee)
    {
        if (!PositionBelongsToDepartment(employee.Department.Trim(), employee.Position.Trim())) return false;
        if (employee.Id == 0) db.Employees.Add(employee);
        else
        {
            var existing = db.Employees.Find(employee.Id);
            if (existing is null) return false;
            var fullName = employee.FullName.Trim();
            var nameChanged = existing.FullName != fullName;
            existing.Code = employee.Code.Trim();
            existing.FullName = fullName;
            existing.Email = employee.Email.Trim().ToLowerInvariant();
            existing.Phone = (employee.Phone ?? "").Trim();
            existing.Department = (employee.Department ?? "").Trim();
            existing.Position = (employee.Position ?? "").Trim();
            existing.StartDate = employee.StartDate.Date;
            existing.Salary = employee.Salary;
            existing.IsActive = employee.IsActive;
            existing.EndDate = employee.IsActive ? null : (employee.EndDate ?? existing.EndDate ?? DateTime.Today);
            if (nameChanged)
            {
                foreach (var attendance in db.Attendances.Where(x => x.EmployeeId == employee.Id)) attendance.EmployeeName = fullName;
                foreach (var payroll in db.Payrolls.Where(x => x.EmployeeId == employee.Id && x.Status != "Đã công bố")) payroll.EmployeeName = fullName;
            }
        }
        db.SaveChanges();
        return true;
    }

    public void DeleteEmployee(int id)
    {
        var employee = db.Employees.Find(id);
        if (employee is null) return;
        employee.IsActive = false;
        employee.EndDate ??= DateTime.Today;
        db.SaveChanges();
    }

    public bool SaveAttendance(int employeeId, DateTime date, string status, decimal overtimeHours, decimal nightHours = 0, decimal nightOvertimeHours = 0, bool isLate = false, bool leftEarly = false)
    {
        var employee = db.Employees.AsNoTracking().FirstOrDefault(x => x.Id == employeeId);
        if (employee is null) return false;
        var period = date.ToString("MM/yyyy");
        if (db.Payrolls.Any(x => x.EmployeeId == employeeId && x.Period == period && x.Status == "Đã công bố")) return false;
        var attendance = db.Attendances.FirstOrDefault(x => x.EmployeeId == employeeId && x.Date.Date == date.Date);
        if (attendance is null)
        {
            attendance = new Attendance { EmployeeId = employee.Id, EmployeeName = employee.FullName, Date = date.Date };
            db.Attendances.Add(attendance);
        }
        attendance.EmployeeName = employee.FullName;
        attendance.Status = status;
        attendance.IsLate = status == "Có mặt" && isLate;
        attendance.LeftEarly = status == "Có mặt" && leftEarly;
        attendance.OvertimeHours = overtimeHours;
        attendance.NightHours = nightHours;
        attendance.NightOvertimeHours = nightOvertimeHours;
        var draft = db.Payrolls.FirstOrDefault(x => x.EmployeeId == employeeId && x.Period == period && x.IsCalculated && (x.Status == "Bản nháp" || x.Status == "Cần tính lại" || x.Status == "Cần điều chỉnh"));
        if (draft is not null) draft.Status = "Cần tính lại";
        db.SaveChanges();
        return true;
    }

    public Payroll? FindPayroll(int id) => db.Payrolls.AsNoTracking().FirstOrDefault(x => x.Id == id);

    public bool SaveDepartment(int id, string name, string manager)
    {
        var department = id == 0 ? new Department() : db.Departments.Find(id);
        if (department is null || db.Departments.Any(x => x.Id != id && x.Name == name)) return false;
        var oldName = department.Name;
        department.Name = name.Trim();
        department.Manager = manager.Trim();
        if (id == 0) db.Departments.Add(department);
        else if (oldName != department.Name)
        {
            foreach (var employee in db.Employees.Where(x => x.Department == oldName)) employee.Department = department.Name;
            foreach (var payroll in db.Payrolls.Where(x => x.Department == oldName)) payroll.Department = department.Name;
        }
        db.SaveChanges();
        return true;
    }

    public bool DeleteDepartment(int id)
    {
        var department = db.Departments.Find(id);
        if (department is null || db.Employees.Any(x => x.Department == department.Name)) return false;
        db.Departments.Remove(department);
        db.SaveChanges();
        return true;
    }

    public bool CheckIn(int employeeId, DateTime now)
    {
        var employee = db.Employees.FirstOrDefault(x => x.Id == employeeId && x.IsActive);
        if (employee is null || !IsScheduledWorkingDate(now.Date)) return false;
        var attendance = db.Attendances.FirstOrDefault(x => x.EmployeeId == employeeId && x.Date.Date == now.Date);
        if (attendance?.CheckInAt is not null) return false;
        if (attendance is null)
        {
            attendance = new Attendance { EmployeeId = employee.Id, EmployeeName = employee.FullName, Date = now.Date };
            db.Attendances.Add(attendance);
        }
        attendance.CheckInAt = now;
        attendance.Status = "Có mặt";
        attendance.IsLate = now.TimeOfDay > GetPayrollSettings().WorkdayStart;
        db.SaveChanges();
        return true;
    }

    public bool CheckOut(int employeeId, DateTime now)
    {
        var attendance = db.Attendances.FirstOrDefault(x => x.EmployeeId == employeeId && x.Date.Date == now.Date);
        if (attendance?.CheckInAt is null || attendance.CheckOutAt is not null || now <= attendance.CheckInAt) return false;
        attendance.CheckOutAt = now;
        attendance.LeftEarly = now.TimeOfDay < GetPayrollSettings().WorkdayEnd;
        db.SaveChanges();
        return true;
    }

    public bool IsScheduledWorkingDate(DateTime date)
    {
        var settings = GetPayrollSettings();
        var dayIndex = ((int)date.DayOfWeek + 6) % 7;
        if ((settings.WorkdaysMask & (1 << dayIndex)) == 0) return false;
        return !db.CompanyHolidays.Any(x => x.Date == date.Date);
    }

    public bool CreateLeaveRequest(int employeeId, DateTime start, DateTime end, string leaveType, string reason)
    {
        var employee = db.Employees.FirstOrDefault(x => x.Id == employeeId && x.IsActive);
        if (employee is null || start.Date < DateTime.Today || end.Date < start.Date || end.Year != start.Year || string.IsNullOrWhiteSpace(reason)
            || reason.Length > 500 || !new[] { "Nghỉ phép năm có lương", "Nghỉ không lương", "Nghỉ ốm (chế độ BHXH)" }.Contains(leaveType)) return false;
        if (db.LeaveRequests.Any(x => x.EmployeeId == employeeId && x.Status != "Từ chối" && x.StartDate.Date <= end.Date && x.EndDate.Date >= start.Date)) return false;
        var settings = GetPayrollSettings();
        var holidays = db.CompanyHolidays.Select(x => x.Date.Date).ToHashSet();
        bool IsWorkday(DateTime day) => (settings.WorkdaysMask & (1 << (((int)day.DayOfWeek + 6) % 7))) != 0 && !holidays.Contains(day.Date);
        var requestedDays = Enumerable.Range(0, (end.Date - start.Date).Days + 1).Select(day => start.Date.AddDays(day)).Count(IsWorkday);
        if (requestedDays == 0) return false;
        if (leaveType == "Nghỉ phép năm có lương")
        {
            var year = start.Year;
            var monthsEligible = employee.StartDate.Year == year ? 13 - employee.StartDate.Month : 12;
            var completedYears = year - employee.StartDate.Year;
            var anniversary = employee.StartDate.AddYears(completedYears);
            var serviceYears = Math.Max(0, completedYears - (start.Date < anniversary.Date ? 1 : 0));
            var entitlement = decimal.Floor((12 + serviceYears / 5) * monthsEligible / 12m * 100) / 100m;
            var used = db.LeaveRequests.Where(x => x.EmployeeId == employeeId && x.LeaveType == "Nghỉ phép năm có lương"
                    && x.Status != "Từ chối" && x.StartDate.Year == year)
                .AsEnumerable().Sum(x => Enumerable.Range(0, (x.EndDate.Date - x.StartDate.Date).Days + 1).Select(day => x.StartDate.Date.AddDays(day)).Count(IsWorkday));
            if (used + requestedDays > entitlement) return false;
        }
        db.LeaveRequests.Add(new LeaveRequest { EmployeeId = employee.Id, EmployeeName = employee.FullName, StartDate = start.Date, EndDate = end.Date, LeaveType = leaveType, Reason = reason.Trim() });
        db.SaveChanges();
        return true;
    }

    public bool ReviewLeave(int id, bool approve, string note)
    {
        var request = db.LeaveRequests.FirstOrDefault(x => x.Id == id && x.Status == "Chờ duyệt");
        if (request is null) return false;
        if (approve)
        {
            var employee = db.Employees.FirstOrDefault(x => x.Id == request.EmployeeId && x.IsActive);
            if (employee is null) return false;
            var settings = GetPayrollSettings();
            var holidays = db.CompanyHolidays.Select(x => x.Date.Date).ToHashSet();
            bool IsWorkday(DateTime day) => (settings.WorkdaysMask & (1 << (((int)day.DayOfWeek + 6) % 7))) != 0 && !holidays.Contains(day.Date);
            var dates = Enumerable.Range(0, (request.EndDate.Date - request.StartDate.Date).Days + 1).Select(day => request.StartDate.Date.AddDays(day)).Where(IsWorkday).ToList();
            foreach (var day in dates)
            {
                var existing = db.Attendances.FirstOrDefault(x => x.EmployeeId == request.EmployeeId && x.Date.Date == day.Date);
                if (existing is not null && existing.Status is not ("Nghỉ phép" or "Vắng mặt")) return false;
                if (existing is null)
                    db.Attendances.Add(new Attendance { EmployeeId = employee.Id, EmployeeName = employee.FullName, Date = day.Date, Status = request.LeaveType == "Nghỉ phép năm có lương" ? "Nghỉ phép" : "Vắng mặt" });
                else existing.Status = request.LeaveType == "Nghỉ phép năm có lương" ? "Nghỉ phép" : "Vắng mặt";
            }
        }
        request.Status = approve ? "Đã duyệt" : "Từ chối";
        request.AdminNote = (note ?? "").Trim();
        db.SaveChanges();
        return true;
    }

    public bool CreateProfileChangeRequest(int employeeId, string email, string phone, string reason)
    {
        var employee = db.Employees.FirstOrDefault(x => x.Id == employeeId && x.IsActive);
        if (employee is null || string.IsNullOrWhiteSpace(reason) || email.Length > 160 || phone.Length > 30) return false;
        try { _ = new MailAddress(email); } catch { return false; }
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (db.AppUsers.Any(x => x.Email == normalizedEmail && x.EmployeeId != employeeId)
            || db.Employees.Any(x => x.Email == normalizedEmail && x.Id != employeeId)) return false;
        if (db.ProfileChangeRequests.Any(x => x.EmployeeId == employeeId && x.Status == "Chờ duyệt")) return false;
        db.ProfileChangeRequests.Add(new ProfileChangeRequest { EmployeeId = employeeId, EmployeeName = employee.FullName, ProposedEmail = email.Trim().ToLowerInvariant(), ProposedPhone = phone.Trim(), Reason = reason.Trim() });
        db.SaveChanges();
        return true;
    }

    public List<ProfileChangeRequest> ProfileRequests(bool pendingOnly = false) => db.ProfileChangeRequests.AsNoTracking()
        .Where(x => !pendingOnly || x.Status == "Chờ duyệt").OrderByDescending(x => x.CreatedAt).ToList();

    public List<ProfileChangeRequest> ProfileRequestsForEmployee(int employeeId) => db.ProfileChangeRequests.AsNoTracking()
        .Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.CreatedAt).ToList();

    public List<LeaveRequest> LeaveRequestsForEmployee(int employeeId) => db.LeaveRequests.AsNoTracking()
        .Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.CreatedAt).ToList();

    public bool ReviewProfileChange(int id, bool approve)
    {
        var request = db.ProfileChangeRequests.FirstOrDefault(x => x.Id == id && x.Status == "Chờ duyệt");
        if (request is null) return false;
        if (approve)
        {
            if (db.AppUsers.Any(x => x.Email == request.ProposedEmail && x.EmployeeId != request.EmployeeId)
                || db.Employees.Any(x => x.Email == request.ProposedEmail && x.Id != request.EmployeeId)) return false;
            var employee = db.Employees.Find(request.EmployeeId);
            if (employee is null) return false;
            employee.Email = request.ProposedEmail;
            employee.Phone = request.ProposedPhone;
            var account = db.AppUsers.FirstOrDefault(x => x.EmployeeId == request.EmployeeId);
            if (account is not null) account.Email = request.ProposedEmail;
        }
        request.Status = approve ? "Đã duyệt" : "Từ chối";
        db.SaveChanges();
        return true;
    }

    public bool CreatePasswordResetRequest(string email)
    {
        var account = db.AppUsers.FirstOrDefault(x => x.Email == email.Trim().ToLowerInvariant() && x.Role == "Employee" && x.IsEnabled);
        if (account?.EmployeeId is not int employeeId) return false;
        if (db.PasswordResetRequests.Any(x => x.EmployeeId == employeeId && x.Status == "Chờ xử lý")) return true;
        db.PasswordResetRequests.Add(new PasswordResetRequest { EmployeeId = employeeId, EmployeeName = account.FullName, Email = account.Email });
        db.SaveChanges();
        return true;
    }

    public List<LeaveRequest> LeaveRequests(bool pendingOnly = false) => db.LeaveRequests.AsNoTracking()
        .Where(x => !pendingOnly || x.Status == "Chờ duyệt").OrderByDescending(x => x.CreatedAt).ToList();

    public List<PasswordResetRequest> PasswordResetRequests(bool pendingOnly = false) => db.PasswordResetRequests.AsNoTracking()
        .Where(x => !pendingOnly || x.Status == "Chờ xử lý").OrderByDescending(x => x.CreatedAt).ToList();

    public void CompletePasswordResetRequest(int id)
    {
        var request = db.PasswordResetRequests.Find(id);
        if (request is null) return;
        request.Status = "Đã cấp lại";
        db.SaveChanges();
    }

    public PayrollSettings GetPayrollSettings() => db.PayrollSettings.FirstOrDefault(x => x.Id == 1) ?? new PayrollSettings { Id = 1 };

    public bool SavePayrollSettings(int[] workdays, decimal hoursPerDay, TimeSpan workdayStart, TimeSpan workdayEnd)
    {
        if (workdays.Length == 0 || workdays.Any(x => x is < 0 or > 6) || hoursPerDay is <= 0 or > 12
            || workdayStart < TimeSpan.Zero || workdayEnd > TimeSpan.FromHours(24) || workdayEnd <= workdayStart
            || hoursPerDay > (decimal)(workdayEnd - workdayStart).TotalHours) return false;
        var settings = GetPayrollSettings();
        settings.WorkdaysMask = workdays.Distinct().Sum(x => 1 << x);
        settings.HoursPerDay = hoursPerDay;
        settings.WorkdayStart = workdayStart;
        settings.WorkdayEnd = workdayEnd;
        if (db.Entry(settings).State == EntityState.Detached) db.PayrollSettings.Add(settings);
        db.SaveChanges();
        return true;
    }

    public List<CompanyHoliday> Holidays(int year) => db.CompanyHolidays.AsNoTracking().Where(x => x.Date.Year == year).OrderBy(x => x.Date).ToList();

    public bool SaveHoliday(DateTime date, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) return false;
        var holiday = db.CompanyHolidays.FirstOrDefault(x => x.Date == date.Date);
        if (holiday is not null) holiday.Name = name.Trim();
        else db.CompanyHolidays.Add(new CompanyHoliday { Date = date.Date, Name = name.Trim() });
        db.SaveChanges();
        return true;
    }

    public void DeleteHoliday(int id)
    {
        var holiday = db.CompanyHolidays.Find(id);
        if (holiday is null) return;
        db.CompanyHolidays.Remove(holiday);
        db.SaveChanges();
    }

    public (Payroll? Payroll, string? Error) CalculatePayroll(int employeeId, string period, decimal allowance, decimal bonus,
        decimal insuranceBase, decimal personalIncomeTax, decimal advanceDeduction, decimal otherDeductions, string otherDeductionReason, bool insuranceEligible = true)
    {
        if (!DateTime.TryParseExact(period, "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var month))
            return (null, "Kỳ lương không đúng định dạng MM/yyyy.");
        var first = new DateTime(month.Year, month.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        if (first >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return (null, "Chỉ lập lương cho kỳ đã kết thúc để chốt đủ dữ liệu công.");
        if (new[] { allowance, bonus, insuranceBase, personalIncomeTax, advanceDeduction, otherDeductions }.Any(x => x < 0))
            return (null, "Các khoản tiền không được âm.");
        if (otherDeductions > 0 && string.IsNullOrWhiteSpace(otherDeductionReason)) return (null, "Cần ghi căn cứ cho khoản khấu trừ khác.");
        if ((otherDeductionReason?.Length ?? 0) > 300) return (null, "Nội dung căn cứ khấu trừ quá dài.");
        var employee = db.Employees.FirstOrDefault(x => x.Id == employeeId);
        if (employee is null) return (null, "Không tìm thấy nhân viên.");
        var settings = GetPayrollSettings();
        var holidays = db.CompanyHolidays.Select(x => x.Date).ToHashSet();
        bool Scheduled(DateTime date) => (settings.WorkdaysMask & (1 << (((int)date.DayOfWeek + 6) % 7))) != 0;
        var scheduledDates = Enumerable.Range(0, (last - first).Days + 1).Select(day => first.AddDays(day))
            .Where(x => Scheduled(x) && !holidays.Contains(x.Date)).ToList();
        var expectedDates = scheduledDates.Where(x => x >= employee.StartDate.Date && (employee.EndDate == null || x <= employee.EndDate.Value.Date)).ToList();
        if (scheduledDates.Count == 0 || expectedDates.Count == 0) return (null, "Kỳ này không có ngày làm việc theo lịch của nhân viên.");
        var attendance = db.Attendances.Where(x => x.EmployeeId == employeeId && x.Date >= first && x.Date <= last).ToList();
        var byDate = attendance.GroupBy(x => x.Date.Date).ToDictionary(x => x.Key, x => x.First());
        var missing = expectedDates.Where(x => !byDate.ContainsKey(x.Date)).ToList();
        if (missing.Count > 0) return (null, $"Chưa chốt chấm công đủ: thiếu {missing.Count} ngày làm việc. Hãy ghi nhận có mặt, nghỉ phép hoặc vắng mặt trước khi lập lương.");
        var unknown = expectedDates.Select(x => byDate[x.Date].Status).FirstOrDefault(x => x is not ("Có mặt" or "Đi muộn" or "Nghỉ phép" or "Vắng mặt"));
        if (unknown is not null) return (null, "Có trạng thái chấm công chưa phân loại; hãy sửa bảng công trước khi tính lương.");
        var paidDays = expectedDates.Count(x => byDate[x.Date].Status is "Có mặt" or "Đi muộn" or "Nghỉ phép");
        var unpaidDays = expectedDates.Count(x => byDate[x.Date].Status == "Vắng mặt");
        attendance = attendance.Where(x => x.Date.Date >= employee.StartDate.Date && (employee.EndDate == null || x.Date.Date <= employee.EndDate.Value.Date)).ToList();
        var monthlyOvertime = attendance.Sum(x => x.OvertimeHours);
        if (monthlyOvertime > 40) return (null, "Tổng tăng ca vượt 40 giờ/tháng. Hãy rà soát dữ liệu và điều kiện ngoại lệ trước khi lập lương.");
        var yearStart = new DateTime(month.Year, 1, 1);
        var yearEnd = yearStart.AddYears(1);
        var yearlyOvertime = db.Attendances.Where(x => x.EmployeeId == employeeId && x.Date >= yearStart && x.Date < yearEnd).Select(x => x.OvertimeHours).ToList().Sum();
        if (yearlyOvertime > 200) return (null, "Tổng tăng ca vượt 200 giờ/năm theo giới hạn mặc định. Trường hợp thuộc diện ngoại lệ cần thiết lập/quy trình riêng trước khi tính.");
        if (attendance.Any(x => x.NightOvertimeHours > x.OvertimeHours || x.NightHours > settings.HoursPerDay))
            return (null, "Giờ làm đêm hoặc tăng ca đêm không khớp với tổng giờ; hãy rà soát bảng công.");
        var overtimeRows = attendance.Where(x => x.OvertimeHours > 0).ToList();
        if (overtimeRows.Any(x => x.Status is not ("Có mặt" or "Đi muộn")))
            return (null, "Không thể tính tăng ca cho ngày nghỉ, vắng mặt hoặc nghỉ phép; hãy sửa bảng công.");
        decimal regularOt = 0, restOt = 0, holidayOt = 0, nightHours = 0, nightOt = 0;
        foreach (var row in overtimeRows)
        {
            if (holidays.Contains(row.Date.Date)) holidayOt += row.OvertimeHours;
            else if (Scheduled(row.Date.Date)) regularOt += row.OvertimeHours;
            else restOt += row.OvertimeHours;
            nightOt += row.NightOvertimeHours;
        }
        nightHours = attendance.Sum(x => x.NightHours);
        insuranceBase = insuranceEligible ? (insuranceBase > 0 ? insuranceBase : employee.Salary) : 0;
        var hourly = employee.Salary / (scheduledDates.Count * settings.HoursPerDay);
        var basePay = decimal.Round(employee.Salary * paidDays / expectedDates.Count, 0, MidpointRounding.AwayFromZero);
        var regularOtPay = decimal.Round(hourly * regularOt * settings.RegularOvertimeRate, 0, MidpointRounding.AwayFromZero);
        var restOtPay = decimal.Round(hourly * restOt * settings.RestOvertimeRate, 0, MidpointRounding.AwayFromZero);
        var holidayOtPay = decimal.Round(hourly * holidayOt * settings.HolidayOvertimeRate, 0, MidpointRounding.AwayFromZero);
        var nightPremium = decimal.Round(hourly * nightHours * settings.NightPremiumRate, 0, MidpointRounding.AwayFromZero);
        var nightOvertimePremium = decimal.Round(hourly * nightOt * (settings.NightPremiumRate + settings.NightOvertimeAdditionalRate), 0, MidpointRounding.AwayFromZero);
        var payroll = db.Payrolls.FirstOrDefault(x => x.EmployeeId == employeeId && x.Period == first.ToString("MM/yyyy"));
        if (payroll?.Status == "Đã công bố") return (null, "Phiếu lương đã công bố và bị khóa; chỉ Admin mới được mở lại bằng quy trình điều chỉnh.");
        payroll ??= new Payroll { EmployeeId = employeeId, Period = first.ToString("MM/yyyy") };
        payroll.EmployeeName = employee.FullName;
        payroll.Department = employee.Department;
        payroll.BaseSalary = employee.Salary;
        payroll.Allowance = allowance;
        payroll.Deduction = 0;
        payroll.IsCalculated = true;
        payroll.Status = "Bản nháp";
        payroll.ScheduledWorkDays = expectedDates.Count;
        payroll.PaidDays = paidDays;
        payroll.UnpaidDays = unpaidDays;
        payroll.BasePay = basePay;
        payroll.HoursPerDayUsed = settings.HoursPerDay;
        payroll.HourlyRateUsed = decimal.Round(hourly, 4, MidpointRounding.AwayFromZero);
        payroll.RegularOvertimeRateUsed = settings.RegularOvertimeRate;
        payroll.RestOvertimeRateUsed = settings.RestOvertimeRate;
        payroll.HolidayOvertimeRateUsed = settings.HolidayOvertimeRate;
        payroll.NightPremiumRateUsed = settings.NightPremiumRate;
        payroll.NightOvertimeAdditionalRateUsed = settings.NightOvertimeAdditionalRate;
        payroll.EmployeeInsuranceRateUsed = insuranceEligible ? settings.EmployeeInsuranceRate : 0;
        payroll.EmployerContributionRateUsed = insuranceEligible ? settings.EmployerContributionRate : 0;
        payroll.RegularOvertimeHours = regularOt;
        payroll.RestOvertimeHours = restOt;
        payroll.HolidayOvertimeHours = holidayOt;
        payroll.NightHours = nightHours;
        payroll.NightOvertimeHours = nightOt;
        payroll.RegularOvertimePay = regularOtPay;
        payroll.RestOvertimePay = restOtPay;
        payroll.HolidayOvertimePay = holidayOtPay;
        payroll.NightPremium = nightPremium;
        payroll.NightOvertimePremium = nightOvertimePremium;
        payroll.Bonus = bonus;
        payroll.InsuranceBase = insuranceBase;
        payroll.EmployeeInsurance = decimal.Round(insuranceBase * settings.EmployeeInsuranceRate, 0, MidpointRounding.AwayFromZero);
        payroll.EmployerContributions = decimal.Round(insuranceBase * settings.EmployerContributionRate, 0, MidpointRounding.AwayFromZero);
        payroll.PersonalIncomeTax = personalIncomeTax;
        payroll.AdvanceDeduction = advanceDeduction;
        payroll.OtherDeductions = otherDeductions;
        payroll.OtherDeductionReason = (otherDeductionReason ?? "").Trim();
        payroll.PublishedAt = null;
        if (payroll.NetSalary < 0) return (null, "Các khoản khấu trừ đang lớn hơn thu nhập. Hãy kiểm tra lại trước khi lưu bảng lương.");
        if (db.Entry(payroll).State == EntityState.Detached) db.Payrolls.Add(payroll);
        db.SaveChanges();
        return (payroll, null);
    }

    public bool PublishPayroll(int id)
    {
        var payroll = db.Payrolls.FirstOrDefault(x => x.Id == id && x.IsCalculated && x.Status == "Bản nháp");
        if (payroll is null) return false;
        payroll.Status = "Đã công bố";
        payroll.PublishedAt = DateTime.Now;
        db.SaveChanges();
        return true;
    }

    public bool ReopenPayrollForAdjustment(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 300) return false;
        var payroll = db.Payrolls.FirstOrDefault(x => x.Id == id && x.IsCalculated && x.Status == "Đã công bố");
        if (payroll is null) return false;
        payroll.Status = "Cần điều chỉnh";
        payroll.AdjustmentReason = reason.Trim();
        payroll.AdjustmentCount++;
        db.SaveChanges();
        return true;
    }

    public bool DeletePayrollDraft(int id)
    {
        var payroll = db.Payrolls.FirstOrDefault(x => x.Id == id && x.Status == "Bản nháp");
        if (payroll is null) return false;
        db.Payrolls.Remove(payroll);
        db.SaveChanges();
        return true;
    }
}
