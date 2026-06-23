namespace AstroDashboard.Models;

public class AstronomyData
{
    public string FileName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public char Filter { get; set; }
    public double ExposureSeconds { get; set; }
    
    public double ExposureMinutes => ExposureSeconds / 60.0;
}
