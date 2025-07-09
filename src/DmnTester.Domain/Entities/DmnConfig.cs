namespace DmnTester.Domain.Entities;

public class DmnConfig
{
    public string DecisionId { get; set; } = string.Empty;
    public List<string> DmnPath { get; set; } = new();
    public DmnConfigData Data { get; set; } = new();
}

public class DmnConfigData
{
    public List<DmnInput> Inputs { get; set; } = new();
    public List<DmnTestCase> TestCases { get; set; } = new();
}

public class DmnInput
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class DmnTestCase
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Inputs { get; set; } = new();
    public Dictionary<string, object> ExpectedOutputs { get; set; } = new();
} 