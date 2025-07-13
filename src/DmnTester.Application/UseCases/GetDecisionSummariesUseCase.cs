using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Application.UseCases
{
    public class GetDecisionSummariesUseCase
    {
        private readonly IDmnFileAnalyzer _dmnFileAnalyzer;

        public GetDecisionSummariesUseCase(IDmnFileAnalyzer dmnFileAnalyzer)
        {
            _dmnFileAnalyzer = dmnFileAnalyzer;
        }

        public async Task<List<DecisionSummary>> ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"DMN file not found: {filePath}");
            }

            return await _dmnFileAnalyzer.GetDecisionSummariesAsync(filePath, cancellationToken);
        }
    }
} 