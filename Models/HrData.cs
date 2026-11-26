namespace HumaHr.Models;

public class HrData
{
    public List<Employee> Employees { get; set; } = [];
    public List<Department> Departments { get; set; } = [];
    public List<Attendance> Attendances { get; set; } = [];
    public List<Payroll> Payrolls { get; set; } = [];
}

public class EmployeeNotification
{
    public DateTime Date { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
}
