using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;

namespace DmnTester.Application.UseCases;

public class TestDmnUseCase
{
    private readonly IDmnEngine _dmnEngine;

    public TestDmnUseCase(IDmnEngine dmnEngine)
    {
        _dmnEngine = dmnEngine;
    }

    public async Task<TestResult> ExecuteAsync(string decisionId, string configFile, string dmnPath)
    {
        var config = await _dmnEngine.LoadConfigAsync(configFile);
        var results = new List<DmnEvalResult>();

        foreach (var testCase in config.Data.TestCases)
        {
            var result = await _dmnEngine.EvaluateAsync(dmnPath, decisionId, testCase.Inputs);
            results.Add(result);
        }

        return new TestResult
        {
            DecisionId = decisionId,
            Results = results
        };
    }
}

public class TestResult
{
    public string DecisionId { get; set; } = string.Empty;
    public List<DmnEvalResult> Results { get; set; } = new();
} 