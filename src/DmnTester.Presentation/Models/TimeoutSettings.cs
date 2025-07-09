namespace DmnTester.Presentation.Models;

public class TimeoutSettings
{
    public int RequestTimeout { get; set; } = 30000; // 30 seconds
    public int DmnEvaluationTimeout { get; set; } = 10000; // 10 seconds
    public int BatchTestTimeout { get; set; } = 60000; // 60 seconds
    public int FileReadTimeout { get; set; } = 5000; // 5 seconds
} 