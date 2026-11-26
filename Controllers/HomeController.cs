using Microsoft.AspNetCore.Mvc;
using HumaHr.Models;
using HumaHr.Services;
using Microsoft.AspNetCore.Authorization;

namespace HumaHr.Controllers;

[Authorize]
public class HomeController(HrRepository repository, AccountService accounts) : Controller
{
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });

    public IActionResult Index()
    {
        if (!User.IsInRole("Admin")) return RedirectToAction(nameof(MyProfile));
        var data = repository.Snapshot();
        data.Payrolls = data.Payrolls.Where(x => x.IsCalculated && x.Status == "Đã công bố").ToList();
        var payrollPeriod = DateTime.Today.AddMonths(-1).ToString("MM/yyyy");
        data.Payrolls = data.Payrolls.Where(x => x.Period == payrollPeriod).ToList();
        data.Attendances = data.Attendances.Where(x => x.Date.Date == DateTime.Today).ToList();
        ViewBag.PayrollPeriod = payrollPeriod;
        ViewBag.Data = data;
        ViewBag.EmployeeCountLastMonth = repository.EmployeeCountAt(DateTime.Today.AddMonths(-1));
        return View();
    }

    public IActionResult Employees(string? q, string? department)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var employees = repository.Snapshot().Employees.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) employees = employees.Where(x => x.FullName.Contains(q) || x.Code.Contains(q) || x.Email.Contains(q));
        if (!string.IsNullOrWhiteSpace(department)) employees = employees.Where(x => x.Department == department);
        ViewBag.Query = q; ViewBag.Department = department;
        ViewBag.Departments = repository.Snapshot().Departments;
        var result = employees.OrderBy(x => x.Id).ToList();
        foreach (var employee in result)
        {
            var account = accounts.ForEmployee(employee.Id);
            employee.AccountStatus = account is null ? "Chưa cấp" : account.IsEnabled ? "Đã cấp" : "Đã khóa";
        }
        return View(result);
    }

    public IActionResult Employee(int? id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var employee = id is int requestedId ? repository.Snapshot().Employees.FirstOrDefault(x => x.Id == requestedId) : new Employee();
        if (employee is null) return NotFound();
        PopulateEmployeeForm(employee);
        if (id is null) return View(new Employee());
        return View(employee);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveEmployee(Employee employee)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (!ModelState.IsValid) { PopulateEmployeeForm(employee); return View("Employee", employee); }
        var allEmployees = repository.Snapshot().Employees;
        var normalizedEmail = employee.Email.Trim();
        var duplicateCode = allEmployees.Any(x => x.Id != employee.Id && string.Equals(x.Code.Trim(), employee.Code.Trim(), StringComparison.OrdinalIgnoreCase));
        var duplicateEmail = allEmployees.Any(x => x.Id != employee.Id && string.Equals(x.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase));
        if (duplicateCode)
            ModelState.AddModelError(nameof(employee.Code), "Mã nhân viên này đã được sử dụng.");
        if (duplicateEmail || accounts.EmailInUseByOtherEmployee(normalizedEmail, employee.Id)
            || (employee.CreateAccount && (accounts.EmployeeAccountExists(employee.Id) || accounts.EmailExists(normalizedEmail))))
            ModelState.AddModelError(nameof(employee.Email), "Email này đã được dùng cho một nhân viên hoặc tài khoản khác.");
        if (!repository.PositionBelongsToDepartment((employee.Department ?? "").Trim(), (employee.Position ?? "").Trim()))
            ModelState.AddModelError(nameof(employee.Position), "Hãy chọn chức vụ thuộc phòng ban đã chọn.");
        if (!ModelState.IsValid)
        {
            PopulateEmployeeForm(employee);
            return View("Employee", employee);
        }
        if (!repository.SaveEmployee(employee))
        {
            TempData["Message"] = "Không tìm thấy hồ sơ nhân viên để lưu. Hãy mở lại danh sách rồi thử lại.";
            return RedirectToAction(nameof(Employees));
        }
        accounts.SyncEmployeeAccount(employee.Id, employee.Email, employee.FullName);
        if (employee.CreateAccount)
        {
            var password = accounts.CreateEmployeeAccount(employee.Id, employee.Email, employee.FullName);
            TempData["Credentials"] = $"Tài khoản đã tạo cho {employee.FullName} ({employee.Email}). Mật khẩu tạm: {password}";
        }
        accounts.SetEmployeeActive(employee.Id, employee.IsActive);
        TempData["Message"] = "Đã lưu thông tin nhân viên.";
        return RedirectToAction(nameof(Employees));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteEmployee(int id) { if (!User.IsInRole("Admin")) return Forbid(); accounts.SetEmployeeActive(id, false); repository.DeleteEmployee(id); TempData["Message"] = "Đã cho nhân viên nghỉ việc và khóa tài khoản."; return RedirectToAction(nameof(Employees)); }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ResetEmployeePassword(int id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var employee = repository.Snapshot().Employees.FirstOrDefault(x => x.Id == id);
        var password = accounts.ResetEmployeePassword(id);
        if (employee is null || password is null)
            TempData["Message"] = "Nhân viên chưa có tài khoản để cấp lại mật khẩu.";
        else
            TempData["Credentials"] = $"Email đăng nhập: {employee.Email} · Mật khẩu tạm mới: {password}. Nhân viên sẽ phải đổi mật khẩu khi đăng nhập.";
        return RedirectToAction(nameof(Employees));
    }

    public IActionResult Departments() { if (!User.IsInRole("Admin")) return Forbid(); var data=repository.Snapshot(); ViewBag.Employees=data.Employees; ViewBag.Positions=repository.DepartmentPositions(); return View(data.Departments); }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveDepartmentPosition(int departmentId, string name)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.SaveDepartmentPosition(departmentId, name)
            ? "Đã thêm chức vụ vào phòng ban."
            : "Chức vụ không hợp lệ hoặc đã có trong phòng ban này.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteDepartmentPosition(int id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.DeleteDepartmentPosition(id)
            ? "Đã xóa chức vụ khỏi phòng ban."
            : "Không thể xóa chức vụ đang được nhân viên sử dụng.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveDepartment(int id, string name, string manager)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80 || (manager?.Length ?? 0) > 120)
            TempData["Message"] = "Tên phòng ban hoặc người quản lý không hợp lệ.";
        else if (!repository.SaveDepartment(id, name, manager ?? ""))
            TempData["Message"] = "Tên phòng ban đã tồn tại hoặc không thể cập nhật.";
        else TempData["Message"] = "Đã lưu phòng ban.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteDepartment(int id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.DeleteDepartment(id)
            ? "Đã xóa phòng ban."
            : "Không thể xóa phòng ban đang có nhân viên.";
        return RedirectToAction(nameof(Departments));
    }

    public IActionResult MyProfile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var employeeId = accounts.Find(userId)?.EmployeeId;
        var employee = employeeId is int id ? repository.Snapshot().Employees.FirstOrDefault(x => x.Id == id) : null;
        return employee is null ? Forbid() : View(employee);
    }

    public IActionResult MyAttendance()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        var data = repository.Snapshot();
        var history = data.Attendances
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.Date).ToList();
        var employee = data.Employees.First(x => x.Id == employeeId);
        var todayRecord = history.FirstOrDefault(x => x.Date.Date == DateTime.Today);
        var scheduledToday = repository.IsScheduledWorkingDate(DateTime.Today);
        ViewBag.Employee = employee;
        ViewBag.CanCheckIn = employee.IsActive && scheduledToday && todayRecord?.CheckInAt is null;
        ViewBag.CanCheckOut = employee.IsActive && todayRecord?.CheckInAt is not null && todayRecord.CheckOutAt is null;
        ViewBag.PunchNotice = !scheduledToday
            ? "Hôm nay không nằm trong lịch làm việc. Nếu có ca cuối tuần hoặc ngày lễ, quản lý sẽ ghi nhận công."
            : todayRecord?.CheckInAt is not null && todayRecord.CheckOutAt is not null
                ? "Bạn đã chấm công vào và ra hôm nay."
                : todayRecord?.CheckInAt is not null ? "Bạn đã chấm công vào; hãy chấm công ra khi kết thúc ca." : "";
        return View(history);
    }

    public IActionResult MyPayroll()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        var history = repository.Snapshot().Payrolls
            .Where(x => x.EmployeeId == employeeId && x.IsCalculated && x.Status == "Đã công bố")
            .OrderByDescending(x => x.Period).ToList();
        return View(history);
    }

    private bool TryGetCurrentEmployee(out int employeeId)
    {
        employeeId = 0;
        if (User.IsInRole("Admin")) return false;
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = accounts.Find(userId);
        if (user?.EmployeeId is not int id) return false;
        employeeId = id;
        return true;
    }

    public IActionResult MyLeaveRequests()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        return View(repository.LeaveRequestsForEmployee(employeeId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SubmitLeaveRequest(DateTime startDate, DateTime endDate, string leaveType, string reason)
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        TempData["Message"] = repository.CreateLeaveRequest(employeeId, startDate, endDate, leaveType, reason)
            ? "Đã gửi đơn nghỉ phép để quản lý duyệt."
            : "Thông tin chưa hợp lệ hoặc bị trùng với đơn đang chờ duyệt.";
        return RedirectToAction(nameof(MyLeaveRequests));
    }

    public IActionResult MyProfileChange()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        var employee = repository.Snapshot().Employees.First(x => x.Id == employeeId);
        ViewBag.Requests = repository.ProfileRequestsForEmployee(employeeId);
        return View(employee);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SubmitProfileChange(string email, string phone, string reason)
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        TempData["Message"] = repository.CreateProfileChangeRequest(employeeId, email ?? "", phone ?? "", reason ?? "")
            ? "Đã gửi yêu cầu cập nhật hồ sơ."
            : "Thông tin không hợp lệ, email đã được sử dụng hoặc bạn đang có yêu cầu chờ duyệt.";
        return RedirectToAction(nameof(MyProfileChange));
    }

    public IActionResult MyNotifications()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        var entries = new List<EmployeeNotification>();
        entries.AddRange(repository.LeaveRequestsForEmployee(employeeId).Select(x => new EmployeeNotification { Date = x.CreatedAt, Message = $"Đơn nghỉ phép {x.StartDate:dd/MM/yyyy}–{x.EndDate:dd/MM/yyyy}", Status = x.Status + (string.IsNullOrWhiteSpace(x.AdminNote) ? "" : " · " + x.AdminNote) }));
        entries.AddRange(repository.ProfileRequestsForEmployee(employeeId).Select(x => new EmployeeNotification { Date = x.CreatedAt, Message = "Yêu cầu cập nhật hồ sơ", Status = x.Status }));
        entries.AddRange(repository.Snapshot().Attendances.Where(x => x.EmployeeId == employeeId).Select(x => new EmployeeNotification { Date = x.Date, Message = $"Chấm công: {x.Status}" + (x.CheckInAt is DateTime checkin ? $" · vào {checkin:HH:mm}" : ""), Status = x.CheckOutAt is DateTime checkout ? $"Đã ra lúc {checkout:HH:mm}" : "" }));
        return View(entries.OrderByDescending(x => x.Date).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CheckIn()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        TempData["Message"] = !repository.IsScheduledWorkingDate(DateTime.Today)
            ? "Hôm nay không nằm trong lịch làm việc; ngày nghỉ/ca lễ do quản lý ghi nhận."
            : repository.CheckIn(employeeId, DateTime.Now) ? "Đã ghi nhận giờ vào làm." : "Bạn đã chấm công vào hôm nay hoặc tài khoản không hoạt động.";
        return RedirectToAction(nameof(MyAttendance));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CheckOut()
    {
        if (!TryGetCurrentEmployee(out var employeeId)) return Forbid();
        TempData["Message"] = repository.CheckOut(employeeId, DateTime.Now) ? "Đã ghi nhận giờ ra về." : "Bạn cần chấm công vào trước hoặc đã chấm công ra hôm nay.";
        return RedirectToAction(nameof(MyAttendance));
    }

    public IActionResult AdminRequests()
    {
        if (!User.IsInRole("Admin")) return Forbid();
        ViewBag.Leaves = repository.LeaveRequests();
        ViewBag.ProfileChanges = repository.ProfileRequests();
        ViewBag.PasswordResets = repository.PasswordResetRequests();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReviewLeaveRequest(int id, bool approve, string note = "")
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.ReviewLeave(id, approve, note) ? "Đã cập nhật trạng thái đơn nghỉ." : "Đơn nghỉ không còn chờ duyệt.";
        return RedirectToAction(nameof(AdminRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReviewProfileRequest(int id, bool approve)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.ReviewProfileChange(id, approve)
            ? "Đã cập nhật yêu cầu hồ sơ."
            : "Không thể áp dụng email này hoặc yêu cầu đã được xử lý.";
        return RedirectToAction(nameof(AdminRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ResetFromRequest(int id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var request = repository.PasswordResetRequests().FirstOrDefault(x => x.Id == id);
        var password = request is null ? null : accounts.ResetEmployeePassword(request.EmployeeId);
        if (request is null || password is null) TempData["Message"] = "Yêu cầu đặt lại mật khẩu không hợp lệ.";
        else
        {
            repository.CompletePasswordResetRequest(id);
            TempData["Credentials"] = $"Email đăng nhập: {request.Email} · Mật khẩu tạm mới: {password}. Nhân viên phải đổi mật khẩu sau khi đăng nhập.";
        }
        return RedirectToAction(nameof(AdminRequests));
    }

    public IActionResult Attendance(DateTime? date)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var selectedDate = (date ?? DateTime.Today).Date;
        var data = repository.Snapshot();
        ViewBag.SelectedDate = selectedDate;
        ViewBag.Employees = data.Employees.OrderBy(x => x.FullName).ToList();
        return View(data.Attendances.Where(x => x.Date.Date == selectedDate).OrderBy(x => x.EmployeeName).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveAttendance(int employeeId, DateTime date, string status, decimal overtimeHours, decimal nightHours, decimal nightOvertimeHours)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var isLate = status is "Đi muộn" or "Đi muộn và về sớm";
        var leftEarly = status is "Về sớm" or "Đi muộn và về sớm";
        if (isLate || leftEarly) status = "Có mặt";
        if (date.Date > DateTime.Today || overtimeHours < 0 || overtimeHours > 24 || nightHours < 0 || nightHours > 24
            || nightOvertimeHours < 0 || nightOvertimeHours > overtimeHours || !new[] { "Có mặt", "Đi muộn", "Về sớm", "Đi muộn và về sớm", "Vắng mặt", "Nghỉ phép" }.Contains(status)
            && !isLate && !leftEarly)
        {
            TempData["Message"] = "Ngày hoặc thông tin chấm công không hợp lệ.";
            return RedirectToAction(nameof(Attendance), new { date = date.ToString("yyyy-MM-dd") });
        }
        if (!repository.SaveAttendance(employeeId, date, status, overtimeHours, nightHours, nightOvertimeHours, isLate, leftEarly))
            TempData["Message"] = "Không thể sửa công: nhân viên không tồn tại hoặc phiếu lương kỳ này đã công bố.";
        else
            TempData["Message"] = "Đã cập nhật chấm công. Nếu đã có phiếu nháp, hãy tính lại trước khi công bố.";
        return RedirectToAction(nameof(Attendance), new { date = date.ToString("yyyy-MM-dd") });
    }
    public IActionResult Payroll(string? period, string? month)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var parsed = DateTime.TryParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var monthValue)
            ? monthValue
            : DateTime.TryParseExact(period, "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var periodValue)
                ? periodValue : DateTime.Today.AddMonths(-1);
        var selectedPeriod = parsed.ToString("MM/yyyy");
        var periodStart = new DateTime(parsed.Year, parsed.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var data = repository.Snapshot();
        ViewBag.Period = selectedPeriod;
        ViewBag.MonthValue = periodStart.ToString("yyyy-MM");
        ViewBag.Employees = data.Employees.Where(x => x.StartDate.Date <= periodEnd && (x.EndDate == null || x.EndDate.Value.Date >= periodStart))
            .OrderBy(x => x.FullName).ToList();
        ViewBag.Settings = repository.GetPayrollSettings();
        return View(data.Payrolls.Where(x => x.Period == selectedPeriod).OrderBy(x => x.Department).ThenBy(x => x.EmployeeName).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GeneratePayroll(int employeeId, string period, decimal allowance, decimal bonus, decimal insuranceBase,
        decimal personalIncomeTax, decimal advanceDeduction, decimal otherDeductions, string otherDeductionReason, bool taxReviewed, bool insuranceEligible = true)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (!taxReviewed)
        {
            TempData["Message"] = "Hãy xác nhận đã rà soát thuế TNCN theo hồ sơ của nhân viên trước khi tính lương.";
            return RedirectToAction(nameof(Payroll), new { period });
        }
        var result = repository.CalculatePayroll(employeeId, period, allowance, bonus,
            insuranceBase, personalIncomeTax, advanceDeduction, otherDeductions, otherDeductionReason, insuranceEligible);
        TempData["Message"] = result.Error ?? "Đã tính và lưu bản nháp lương. Kiểm tra chi tiết rồi mới công bố cho nhân viên.";
        return RedirectToAction(nameof(Payroll), new { period });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RecalculatePayroll(int id, bool taxReviewed)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var payroll = repository.FindPayroll(id);
        if (payroll is null || payroll.Status is not ("Cần tính lại" or "Cần điều chỉnh"))
        {
            TempData["Message"] = "Không tìm thấy phiếu cần tính lại.";
            return RedirectToAction(nameof(Payroll));
        }
        if (!taxReviewed)
        {
            TempData["Message"] = "Hãy xác nhận đã rà soát thuế trước khi tính lại phiếu lương.";
            return RedirectToAction(nameof(Payroll), new { period = payroll.Period });
        }
        var result = repository.CalculatePayroll(payroll.EmployeeId, payroll.Period, payroll.Allowance, payroll.Bonus,
            payroll.InsuranceBase, payroll.PersonalIncomeTax, payroll.AdvanceDeduction, payroll.OtherDeductions,
            payroll.OtherDeductionReason, payroll.EmployeeInsuranceRateUsed > 0);
        TempData["Message"] = result.Error ?? "Đã tính lại phiếu lương từ bảng công mới. Kiểm tra chi tiết trước khi công bố.";
        return RedirectToAction(nameof(Payroll), new { period = payroll.Period });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReopenPayrollForAdjustment(int id, string reason)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var payroll = repository.FindPayroll(id);
        if (payroll is null || !repository.ReopenPayrollForAdjustment(id, reason))
        {
            TempData["Message"] = "Không thể mở lại phiếu. Hãy nhập lý do và kiểm tra trạng thái phiếu.";
            return RedirectToAction(nameof(Payroll));
        }
        TempData["Message"] = "Đã mở phiếu để điều chỉnh. Sửa bảng công nếu cần, tính lại rồi công bố lại.";
        return RedirectToAction(nameof(Payroll), new { period = payroll.Period });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdatePayrollDraft(int id, decimal allowance, decimal bonus, decimal insuranceBase,
        decimal personalIncomeTax, decimal advanceDeduction, decimal otherDeductions, string otherDeductionReason,
        bool insuranceEligible, bool taxReviewed)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var payroll = repository.FindPayroll(id);
        if (payroll is null || !payroll.IsCalculated || payroll.Status is not ("Bản nháp" or "Cần tính lại" or "Cần điều chỉnh"))
        {
            TempData["Message"] = "Chỉ có thể sửa các khoản trên phiếu nháp hoặc phiếu đang điều chỉnh.";
            return RedirectToAction(nameof(Payroll));
        }
        if (!taxReviewed)
        {
            TempData["Message"] = "Hãy xác nhận đã rà soát thuế TNCN trước khi lưu lại phiếu.";
            return RedirectToAction(nameof(Payroll), new { period = payroll.Period });
        }
        var result = repository.CalculatePayroll(payroll.EmployeeId, payroll.Period, allowance, bonus,
            insuranceBase, personalIncomeTax, advanceDeduction, otherDeductions, otherDeductionReason, insuranceEligible);
        TempData["Message"] = result.Error ?? "Đã lưu các khoản lương và tính lại từ bảng công.";
        return RedirectToAction(nameof(Payroll), new { period = payroll.Period });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult PublishPayroll(int id, string period)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.PublishPayroll(id) ? "Đã công bố phiếu lương cho nhân viên." : "Không thể công bố phiếu lương này.";
        return RedirectToAction(nameof(Payroll), new { period });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeletePayroll(int id, string period)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.DeletePayrollDraft(id) ? "Đã xóa bản nháp." : "Chỉ có thể xóa phiếu lương dạng bản nháp.";
        return RedirectToAction(nameof(Payroll), new { period });
    }

    public IActionResult PayrollSettings(int? year)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        ViewBag.Settings = repository.GetPayrollSettings();
        ViewBag.Holidays = repository.Holidays(year ?? DateTime.Today.Year);
        ViewBag.Year = year ?? DateTime.Today.Year;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SavePayrollSettings(int[] workdays, decimal hoursPerDay, TimeSpan workdayStart, TimeSpan workdayEnd)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.SavePayrollSettings(workdays, hoursPerDay, workdayStart, workdayEnd)
            ? "Đã lưu lịch làm việc. Lịch này áp dụng cho kỳ lương chưa công bố."
            : "Lịch làm việc hoặc số giờ/ngày không hợp lệ.";
        return RedirectToAction(nameof(PayrollSettings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveCompanyHoliday(DateTime date, string name)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        TempData["Message"] = repository.SaveHoliday(date, name) ? "Đã lưu ngày nghỉ." : "Tên ngày nghỉ không hợp lệ.";
        return RedirectToAction(nameof(PayrollSettings), new { year = date.Year });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteCompanyHoliday(int id, int year)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        repository.DeleteHoliday(id);
        TempData["Message"] = "Đã xóa ngày nghỉ.";
        return RedirectToAction(nameof(PayrollSettings), new { year });
    }
    public IActionResult Reports(string? period)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var data = repository.Snapshot();
        var payrollPeriod = DateTime.TryParseExact(period, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsed)
            ? parsed.ToString("MM/yyyy") : DateTime.Today.AddMonths(-1).ToString("MM/yyyy");
        var activeEmployees = data.Employees.Where(x => x.IsActive).ToList();
        var publishedPayrolls = data.Payrolls.Where(x => x.IsCalculated && x.Status == "Đã công bố" && x.Period == payrollPeriod).ToList();
        ViewBag.PayrollPeriod = payrollPeriod;
        ViewBag.TotalPayroll = publishedPayrolls.Sum(x => x.NetSalary);
        ViewBag.PublishedPayrollCount = publishedPayrolls.Count;
        ViewBag.AverageSalary = activeEmployees.Count == 0 ? 0 : activeEmployees.Average(x => x.Salary);
        ViewBag.ActiveCount = activeEmployees.Count;
        ViewBag.Employees = data.Employees;
        return View(data.Departments.Select(d => new Department { Id=d.Id, Name=d.Name, Manager=d.Manager, Headcount=activeEmployees.Count(e=>e.Department==d.Name) }).ToList());
    }

    public IActionResult Settings()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        ViewBag.CurrentAccount = accounts.Find(userId);
        return View();
    }
    public IActionResult Help() => View();

    public IActionResult Notifications()
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var data = repository.Snapshot();
        var today = DateTime.Today;
        var todayRecords = data.Attendances.Where(x => x.Date.Date == today).ToList();
        ViewBag.MissingAttendance = data.Employees.Where(e => e.IsActive && !todayRecords.Any(a => a.EmployeeId == e.Id)).ToList();
        ViewBag.LateAttendance = todayRecords.Where(x => x.IsLate).ToList();
        ViewBag.EarlyAttendance = todayRecords.Where(x => x.LeftEarly).ToList();
        ViewBag.EmployeeCodes = data.Employees.ToDictionary(x => x.Id, x => x.Code);
        ViewBag.EmployeeNames = data.Employees.ToDictionary(x => x.Id, x => x.FullName);
        return View();
    }

    private void PopulateEmployeeForm(Employee employee)
    {
        ViewBag.Departments = repository.Snapshot().Departments.Select(x => x.Name).ToList();
        ViewBag.PositionsByDepartment = repository.PositionsByDepartment();
        ViewBag.HasAccount = employee.Id > 0 && accounts.EmployeeAccountExists(employee.Id);
    }
}
