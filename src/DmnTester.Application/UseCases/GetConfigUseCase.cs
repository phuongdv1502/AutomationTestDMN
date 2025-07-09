using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;

namespace DmnTester.Application.UseCases;

public class GetConfigUseCase
{
    private readonly IDmnEngine _dmnEngine;

    public GetConfigUseCase(IDmnEngine dmnEngine)
    {
        _dmnEngine = dmnEngine;
    }

    public async Task<DmnConfig> ExecuteAsync(string configFile)
    {
        return await _dmnEngine.LoadConfigAsync(configFile);
    }
} 