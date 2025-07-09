using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;

namespace DmnTester.Application.UseCases;

public class EvaluateDmnUseCase
{
    private readonly IDmnEngine _dmnEngine;

    public EvaluateDmnUseCase(IDmnEngine dmnEngine)
    {
        _dmnEngine = dmnEngine;
    }

    public async Task<DmnEvalResult> ExecuteAsync(string dmnPath, string decisionId, Dictionary<string, object> inputs)
    {
        return await _dmnEngine.EvaluateAsync(dmnPath, decisionId, inputs);
    }
} 