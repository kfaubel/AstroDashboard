namespace AstroDashboard.Models;

public class SummaryRow
{
    public string Filter { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public double TotalExposureMinutes { get; set; }
    public bool IsSubtotal { get; set; }
    public bool IsGrandTotal { get; set; }
}
