namespace DmnTester.Domain.Entities;

public class DmnEvalResult
{
    public string DecisionId { get; set; } = string.Empty;
    public Dictionary<string, object> Outputs { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
} 