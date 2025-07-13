namespace DmnTester.Presentation.Models;

public class TimeoutSettings
{
    public int RequestTimeout { get; set; } = 120000; // 2 minutes
    public int DmnEvaluationTimeout { get; set; } = 30000; // 30 seconds
    public int BatchTestTimeout { get; set; } = 300000; // 5 minutes
    public int FileReadTimeout { get; set; } = 10000; // 10 seconds
}

public class PerformanceSettings
{
    public int MaxFileSizeMB { get; set; } = 100;
    public int MaxRulesPerDecision { get; set; } = 2000;
    public int MaxTotalRules { get; set; } = 15000;
    public int ChunkSizeKB { get; set; } = 512;
    public bool EnableCaching { get; set; } = true;
    public int CacheTimeoutMinutes { get; set; } = 30;
    public int MaxTestCasesPerDecision { get; set; } = 1000;
    public int MaxInputCombinations { get; set; } = 500;
} 