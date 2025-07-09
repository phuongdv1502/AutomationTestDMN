using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;

namespace DmnTester.Application.UseCases;

public class GenerateTestFromDmnUseCase
{
    private readonly IDmnTestGenerator _testGenerator;

    public GenerateTestFromDmnUseCase(IDmnTestGenerator testGenerator)
    {
        _testGenerator = testGenerator;
    }

    public async Task<DmnConfig> ExecuteAsync(string dmnPath, string decisionId)
    {
        return await _testGenerator.GenerateTestConfigFromDmnAsync(dmnPath, decisionId);
    }

    public async Task<List<(string decisionId, DmnConfig testConfig)>> ExecuteAllAsync(string dmnPath)
    {
        return await _testGenerator.GenerateAllTestConfigsFromDmnAsync(dmnPath);
    }
} 