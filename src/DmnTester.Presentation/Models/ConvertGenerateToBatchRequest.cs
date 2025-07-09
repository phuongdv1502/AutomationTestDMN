using Newtonsoft.Json;

namespace DmnTester.Presentation.Models;

public class ConvertGenerateToBatchRequest
{
    [JsonProperty("items")]
    public List<GenerateTestOutput> Items { get; set; } = new();
}

public class GenerateTestOutput
{
    public string DecisionId { get; set; } = string.Empty;
    public TestConfig TestConfig { get; set; } = new();
}

public class TestConfig
{
    public string DecisionId { get; set; } = string.Empty;
    public List<string> DmnPath { get; set; } = new();
    public TestConfigData Data { get; set; } = new();
}

public class TestConfigData
{
    public List<object> Inputs { get; set; } = new();
    public List<SingleTestCase> TestCases { get; set; } = new();
} 