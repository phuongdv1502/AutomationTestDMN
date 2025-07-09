using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;

namespace DmnTester.Application.UseCases;

public class GetConfigsUseCase
{
    private readonly IDmnEngine _dmnEngine;

    public GetConfigsUseCase(IDmnEngine dmnEngine)
    {
        _dmnEngine = dmnEngine;
    }

    public async Task<List<DmnConfig>> ExecuteAsync(string configPath)
    {
        return await _dmnEngine.LoadConfigsAsync(configPath);
    }
} 