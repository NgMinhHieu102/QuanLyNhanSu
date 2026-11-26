using HumaHr.Models;
using Microsoft.EntityFrameworkCore;

namespace HumaHr.Data;

public static class SeedData
{
    // Explicit demo identities: never fill attendance for unrelated employee records.
    private static readonly Dictionary<string, string> DemoEmployees = new()
    {
        ["NV001"] = "minhanh@acme.vn", ["NV002"] = "quocbao@acme.vn",
        ["NV003"] = "thuha@acme.vn", ["NV004"] = "duclong@acme.vn",
        ["NV005"] = "hoangyen@acme.vn", ["NV006"] = "thanhnam@acme.vn",
        ["NV007"] = "ngocmai@acme.vn", ["NV008"] = "giahuy@acme.vn"
    };

    public static int EnsureDemoAttendance(HumaHrDbContext db, DateTime month)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var next = first.AddMonths(1);
        if (next > DateTime.Today) throw new ArgumentException("Chỉ tạo công demo cho tháng đã kết thúc.");
        using var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var settings = db.PayrollSettings.AsNoTracking().Single(x => x.Id == 1);
        var period = first.ToString("MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var rows = BuildDemoAttendance(first, settings,
            db.Employees.AsNoTracking().ToList(),
            db.Attendances.AsNoTracking().Where(x => x.Date >= first && x.Date < next).ToList(),
            db.CompanyHolidays.AsNoTracking().Where(x => x.Date >= first && x.Date < next).ToList(),
            db.Payrolls.AsNoTracking().Where(x => x.Period == period && x.Status == "Đã công bố").Select(x => x.EmployeeId).ToHashSet(),
            db.LeaveRequests.AsNoTracking().Where(x => x.StartDate < next && x.EndDate >= first && x.Status != "Từ chối").ToList());
        db.Attendances.AddRange(rows);
        var affected = rows.Select(x => x.EmployeeId).ToHashSet();
        foreach (var draft in db.Payrolls.Where(x => x.Period == period && x.IsCalculated
                     && (x.Status == "Bản nháp" || x.Status == "Cần tính lại" || x.Status == "Cần điều chỉnh")).ToList())
            if (affected.Contains(draft.EmployeeId)) draft.Status = "Cần tính lại";
        db.SaveChanges();
        transaction.Commit();
        return rows.Count;
    }

    public static List<Attendance> BuildDemoAttendance(DateTime month, PayrollSettings settings,
        IEnumerable<Employee> employees, IEnumerable<Attendance> existing,
        IEnumerable<CompanyHoliday> holidays, ISet<int> publishedEmployeeIds,
        IEnumerable<LeaveRequest> leaveRequests)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var next = first.AddMonths(1);
        var occupied = existing.Select(x => (x.EmployeeId, x.Date.Date)).ToHashSet();
        var holidayDates = holidays.Select(x => x.Date.Date).ToHashSet();
        var leaves = leaveRequests.Where(x => x.Status != "Từ chối").ToList();
        var rows = new List<Attendance>();
        foreach (var employee in employees)
        {
            if (!DemoEmployees.TryGetValue(employee.Code, out var email)
                || !string.Equals(email, employee.Email, StringComparison.OrdinalIgnoreCase)
                || publishedEmployeeIds.Contains(employee.Id)) continue;
            for (var day = first; day < next; day = day.AddDays(1))
            {
                var dayIndex = ((int)day.DayOfWeek + 6) % 7;
                if ((settings.WorkdaysMask & (1 << dayIndex)) == 0 || holidayDates.Contains(day)
                    || day < employee.StartDate.Date || (employee.EndDate.HasValue && day > employee.EndDate.Value.Date)
                    || occupied.Contains((employee.Id, day))
                    || leaves.Any(x => x.EmployeeId == employee.Id && x.StartDate.Date <= day && x.EndDate.Date >= day)) continue;
                rows.Add(new Attendance
                {
                    EmployeeId = employee.Id, EmployeeName = employee.FullName, Date = day,
                    Status = "Có mặt", CheckInAt = day.Add(settings.WorkdayStart),
                    CheckOutAt = day.Add(settings.WorkdayEnd)
                });
                occupied.Add((employee.Id, day));
            }
        }
        return rows;
    }

    public static void EnsureDepartmentPositions(HumaHrDbContext db)
    {
        var defaults = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kinh doanh"] = ["Trưởng phòng kinh doanh", "Phó phòng kinh doanh", "Quản lý kinh doanh", "Chuyên viên kinh doanh", "Nhân viên kinh doanh"],
            ["Công nghệ"] = ["Trưởng phòng công nghệ", "Phó phòng công nghệ", "Quản lý kỹ thuật", "Kỹ sư phần mềm", "Lập trình viên", "QA Engineer", "DevOps Engineer", "UX Designer", "UX/UI Designer", "Product Manager"],
            ["Nhân sự"] = ["Trưởng phòng nhân sự", "Phó phòng nhân sự", "Chuyên viên nhân sự", "Chuyên viên tuyển dụng", "Chuyên viên C&B"],
            ["Tài chính"] = ["Trưởng phòng tài chính", "Phó phòng tài chính", "Kế toán trưởng", "Kế toán viên", "Chuyên viên phân tích", "Chuyên viên phân tích tài chính"],
            ["Marketing"] = ["Trưởng phòng marketing", "Chuyên viên marketing", "Chuyên viên nội dung", "Chuyên viên thiết kế"]
        };

        var departments = db.Departments.ToList();
        var knownDepartmentNames = departments.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var positions = db.DepartmentPositions.ToList();
        foreach (var employee in db.Employees.AsNoTracking().Where(x => x.Department != "" && x.Position != ""))
        {
            var department = departments.FirstOrDefault(x => string.Equals(x.Name, employee.Department, StringComparison.OrdinalIgnoreCase));
            if (department is null) continue;
            if (!positions.Any(x => x.DepartmentId == department.Id && string.Equals(x.Name, employee.Position, StringComparison.OrdinalIgnoreCase)))
            {
                db.DepartmentPositions.Add(new DepartmentPosition { DepartmentId = department.Id, Name = employee.Position.Trim() });
                positions.Add(new DepartmentPosition { DepartmentId = department.Id, Name = employee.Position.Trim() });
            }
        }

        foreach (var (departmentName, names) in defaults)
        {
            var department = departments.FirstOrDefault(x => string.Equals(x.Name, departmentName, StringComparison.OrdinalIgnoreCase));
            if (department is null || !knownDepartmentNames.Contains(department.Name)) continue;
            foreach (var name in names)
            {
                if (positions.Any(x => x.DepartmentId == department.Id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) continue;
                db.DepartmentPositions.Add(new DepartmentPosition { DepartmentId = department.Id, Name = name });
                positions.Add(new DepartmentPosition { DepartmentId = department.Id, Name = name });
            }
        }
        db.SaveChanges();
    }

    public static void Initialize(HumaHrDbContext db)
    {
        if (db.Employees.Any()) return;
        var departments = new[]
        {
            new Department { Name="Kinh doanh", Manager="Nguyễn Minh Anh" },
            new Department { Name="Công nghệ", Manager="Trần Quốc Bảo" },
            new Department { Name="Nhân sự", Manager="Lê Thu Hà" },
            new Department { Name="Tài chính", Manager="Phạm Đức Long" }
        };
        db.Departments.AddRange(departments);
        var employees = new[]
        {
            new Employee { Code="NV001", FullName="Nguyễn Minh Anh", Email="minhanh@acme.vn", Phone="0901234567", Department="Kinh doanh", Position="Trưởng phòng", StartDate=new(2021,3,15), Salary=28000000 },
            new Employee { Code="NV002", FullName="Trần Quốc Bảo", Email="quocbao@acme.vn", Phone="0912345678", Department="Công nghệ", Position="Kỹ sư phần mềm", StartDate=new(2022,6,1), Salary=22000000 },
            new Employee { Code="NV003", FullName="Lê Thu Hà", Email="thuha@acme.vn", Phone="0923456789", Department="Nhân sự", Position="Chuyên viên nhân sự", StartDate=new(2023,1,9), Salary=16000000 },
            new Employee { Code="NV004", FullName="Phạm Đức Long", Email="duclong@acme.vn", Phone="0934567890", Department="Tài chính", Position="Kế toán viên", StartDate=new(2024,2,19), Salary=15000000 },
            new Employee { Code="NV005", FullName="Đỗ Hoàng Yến", Email="hoangyen@acme.vn", Phone="0945678901", Department="Công nghệ", Position="UX Designer", StartDate=new(2024,8,12), Salary=18000000 },
            new Employee { Code="NV006", FullName="Vũ Thành Nam", Email="thanhnam@acme.vn", Phone="0956789012", Department="Kinh doanh", Position="Chuyên viên kinh doanh", StartDate=new(2025,4,7), Salary=14500000 },
            new Employee { Code="NV007", FullName="Bùi Ngọc Mai", Email="ngocmai@acme.vn", Phone="0967890123", Department="Công nghệ", Position="QA Engineer", StartDate=new(2025,9,1), Salary=17000000 },
            new Employee { Code="NV008", FullName="Hoàng Gia Huy", Email="giahuy@acme.vn", Phone="0978901234", Department="Tài chính", Position="Chuyên viên phân tích", StartDate=new(2026,1,5), Salary=19000000 }
        };
        db.Employees.AddRange(employees);
        db.SaveChanges();
        var today=DateTime.Today;
        foreach(var e in employees)
        {
            db.Attendances.Add(new Attendance { EmployeeId=e.Id, EmployeeName=e.FullName, Date=today, Status="Có mặt", IsLate=e.Id==8, OvertimeHours=e.Id%3==0?1.5m:0 });
            db.Payrolls.Add(new Payroll { EmployeeId=e.Id, EmployeeName=e.FullName, Department=e.Department, Period=today.ToString("MM/yyyy"), BaseSalary=e.Salary, Allowance=1000000, Deduction=e.Salary*.08m });
        }
        db.SaveChanges();
    }
}
