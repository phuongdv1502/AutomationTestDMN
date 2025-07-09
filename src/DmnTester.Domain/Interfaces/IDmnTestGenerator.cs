using DmnTester.Domain.Entities;

namespace DmnTester.Domain.Interfaces;

public interface IDmnTestGenerator
{
    Task<DmnConfig> GenerateTestConfigFromDmnAsync(string dmnPath, string decisionId);
    Task<List<(string decisionId, DmnConfig testConfig)>> GenerateAllTestConfigsFromDmnAsync(string dmnPath);
} 