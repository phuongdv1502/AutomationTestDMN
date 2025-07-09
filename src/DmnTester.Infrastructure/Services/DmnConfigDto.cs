using System.Text.Json.Serialization;

namespace DmnTester.Infrastructure.Services;

public class DmnConfigDto
{
    [JsonPropertyName("decisionId")]
    public string DecisionId { get; set; } = string.Empty;
    
    [JsonPropertyName("dmnPath")]
    public List<string> DmnPath { get; set; } = new();
    
    [JsonPropertyName("isActive")]
    public string? IsActive { get; set; } // Bỏ qua trường này
    
    [JsonPropertyName("data")]
    public DmnConfigDataDto Data { get; set; } = new();
}

public class DmnConfigDataDto
{
    [JsonPropertyName("inputs")]
    public List<DmnInputDto> Inputs { get; set; } = new();
    
    [JsonPropertyName("testCases")]
    public List<DmnTestCaseDto> TestCases { get; set; } = new();
    
    [JsonPropertyName("variables")]
    public List<object> Variables { get; set; } = new(); // Bỏ qua trường này
}

public class DmnInputDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new();
}

public class DmnTestCaseDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("inputs")]
    public Dictionary<string, object> Inputs { get; set; } = new();
    
    [JsonPropertyName("expectedOutputs")]
    public Dictionary<string, object> ExpectedOutputs { get; set; } = new();
    
    [JsonPropertyName("results")]
    public List<DmnTestResultDto> Results { get; set; } = new();
}

public class DmnTestResultDto
{
    [JsonPropertyName("outputs")]
    public Dictionary<string, object> Outputs { get; set; } = new();
    
    [JsonPropertyName("rowIndex")]
    public string RowIndex { get; set; } = string.Empty;
} 