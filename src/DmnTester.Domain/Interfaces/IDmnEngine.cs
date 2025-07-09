using DmnTester.Domain.Entities;

namespace DmnTester.Domain.Interfaces;

public interface IDmnEngine
{
    Task<List<DmnConfig>> LoadConfigsAsync(string configPath);
    Task<DmnConfig> LoadConfigAsync(string configFile);
    Task<DmnEvalResult> EvaluateAsync(string dmnPath, string decisionId, Dictionary<string, object> inputs);
} 