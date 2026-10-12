namespace Kaarigar.Models;

public class ScheduleShiftItemViewModel
{
    public int JobApplicationId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string EmployerDisplayName { get; set; } = "Registered Employer";
    public DateTime ShiftDt { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? LocationAddressTxt { get; set; }
    public string? MapUrl { get; set; }
    public string? EmployerContactNbr { get; set; }
    public string StatusCd { get; set; } = "PENDING";

    /// <summary>True while the shift hasn't started yet (worker can still check in).</summary>
    public bool CanCheckIn => StatusCd is "PENDING" or "EMPLOYER_VIEWED" or "EMPLOYER_CONTACTED";
}

public class EmployeeScheduleViewModel
{
    public List<ScheduleShiftItemViewModel> Today { get; set; } = new();
    public List<ScheduleShiftItemViewModel> Tomorrow { get; set; } = new();
    public List<ScheduleShiftItemViewModel> NextThreeDays { get; set; } = new();
}

public class EmployeeEarningsViewModel
{
    public decimal ThisMonthTotalAmt { get; set; }
    public int ThisMonthJobsCompleted { get; set; }
    public decimal ThisMonthTotalHours { get; set; }
    public decimal ThisMonthAvgHourlyRate { get; set; }

    public List<EarningsHistoryItemViewModel> History { get; set; } = new();
}

public class EarningsHistoryItemViewModel
{
    public string JobTitle { get; set; } = string.Empty;
    public string EmployerDisplayName { get; set; } = "Registered Employer";
    public DateTime InvoiceDt { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal AmountAmt { get; set; }
}
