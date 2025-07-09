namespace DmnTester.Presentation.Models;

public class EvaluateRequest
{
    public string DmnPath { get; set; } = string.Empty;
    public string DecisionId { get; set; } = string.Empty;
    public Dictionary<string, object> Inputs { get; set; } = new();
} 