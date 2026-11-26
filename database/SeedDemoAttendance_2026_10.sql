-- DU LIEU GIA LAP de demo tinh luong 10/2026. Khong phai cham cong thuc te.
-- SSMS: mo file, kiem tra database HumaHrDb, nhan Execute (F5).
-- Chi bo sung cong thieu cho 8 nhan vien mau co dung ma VA email.
-- Khong sua cong da co; bo qua luong da cong bo va ngay co don nghi chua bi tu choi.
USE [HumaHrDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @First date = '20261001', @Next date = '20261101', @Period nvarchar(7) = N'10/2026';
    IF @Next > CAST(GETDATE() AS date)
        THROW 50001, N'Chỉ tạo công demo cho tháng đã kết thúc.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.PayrollSettings WHERE Id = 1)
        THROW 50002, N'Thiếu cấu hình PayrollSettings Id = 1. Hãy chạy ứng dụng trước.', 1;
    DECLARE @Added TABLE (EmployeeId int, [Date] datetime2);
    ;WITH Days AS (
        SELECT @First AS [Date]
        UNION ALL SELECT DATEADD(day, 1, [Date]) FROM Days WHERE DATEADD(day, 1, [Date]) < @Next
    ), DemoEmployees AS (
        SELECT * FROM (VALUES
            (N'NV001', N'minhanh@acme.vn'), (N'NV002', N'quocbao@acme.vn'),
            (N'NV003', N'thuha@acme.vn'), (N'NV004', N'duclong@acme.vn'),
            (N'NV005', N'hoangyen@acme.vn'), (N'NV006', N'thanhnam@acme.vn'),
            (N'NV007', N'ngocmai@acme.vn'), (N'NV008', N'giahuy@acme.vn')
        ) v(Code, Email)
    )
    INSERT INTO dbo.Attendances
        (EmployeeId, EmployeeName, [Date], Status, IsLate, LeftEarly,
         OvertimeHours, CheckInAt, CheckOutAt, NightHours, NightOvertimeHours)
    OUTPUT inserted.EmployeeId, inserted.[Date] INTO @Added
    SELECT e.Id, e.FullName, d.[Date], N'Có mặt', 0, 0, 0,
        DATEADD(second, DATEDIFF(second, CAST('00:00:00' AS time), s.WorkdayStart), CAST(d.[Date] AS datetime2)),
        DATEADD(second, DATEDIFF(second, CAST('00:00:00' AS time), s.WorkdayEnd), CAST(d.[Date] AS datetime2)), 0, 0
    FROM dbo.Employees e
    JOIN DemoEmployees demo ON demo.Code = e.Code AND demo.Email = e.Email
    CROSS JOIN Days d
    CROSS JOIN dbo.PayrollSettings s
    WHERE s.Id = 1
      -- 1900-01-01 was Monday; independent of SET DATEFIRST / language.
      AND (s.WorkdaysMask & CAST(POWER(2, DATEDIFF(day, '19000101', d.[Date]) % 7) AS int)) <> 0
      AND d.[Date] >= CAST(e.StartDate AS date)
      AND (e.EndDate IS NULL OR d.[Date] <= CAST(e.EndDate AS date))
      AND NOT EXISTS (SELECT 1 FROM dbo.CompanyHolidays h WHERE h.[Date] = d.[Date])
      AND NOT EXISTS (SELECT 1 FROM dbo.Payrolls p WHERE p.EmployeeId = e.Id AND p.Period = @Period AND p.Status = N'Đã công bố')
      AND NOT EXISTS (SELECT 1 FROM dbo.Attendances a WITH (UPDLOCK, HOLDLOCK)
                      WHERE a.EmployeeId = e.Id AND a.[Date] >= d.[Date] AND a.[Date] < DATEADD(day, 1, d.[Date]))
      AND NOT EXISTS (SELECT 1 FROM dbo.LeaveRequests l WHERE l.EmployeeId = e.Id AND l.Status <> N'Từ chối'
                      AND CAST(l.StartDate AS date) <= d.[Date] AND CAST(l.EndDate AS date) >= d.[Date])
    OPTION (MAXRECURSION 31);

    UPDATE p SET Status = N'Cần tính lại'
    FROM dbo.Payrolls p
    WHERE p.Period = @Period AND p.IsCalculated = 1
      AND p.Status IN (N'Bản nháp', N'Cần tính lại', N'Cần điều chỉnh')
      AND EXISTS (SELECT 1 FROM @Added a WHERE a.EmployeeId = p.EmployeeId);
    COMMIT TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    SELECT COUNT(*) AS SoDongCongMauDaThem FROM @Added;
    SELECT e.Code, e.FullName, COUNT(*) AS SoNgayDaCoCong
    FROM dbo.Attendances a JOIN dbo.Employees e ON e.Id = a.EmployeeId
    WHERE a.[Date] >= @First AND a.[Date] < @Next
    GROUP BY e.Code, e.FullName ORDER BY e.Code;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    THROW;
END CATCH;
GO
