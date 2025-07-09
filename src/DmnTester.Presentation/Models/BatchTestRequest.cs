namespace DmnTester.Presentation.Models;

public class BatchTestRequest
{
    public List<BatchTestItem> Items { get; set; } = new();
}

public class BatchTestItem
{
    public string DmnFileName { get; set; } = string.Empty;
    public string DecisionId { get; set; } = string.Empty;
    public List<SingleTestCase> TestCases { get; set; } = new();
}

public class SingleTestCase
{
    public Dictionary<string, object> Inputs { get; set; } = new();
    public Dictionary<string, object> ExpectedOutputs { get; set; } = new();
    public string? Name { get; set; }
} 