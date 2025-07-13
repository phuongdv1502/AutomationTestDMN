using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Application.UseCases
{
    public class ExtractDecisionUseCase
    {
        private readonly IDmnFileAnalyzer _dmnFileAnalyzer;

        public ExtractDecisionUseCase(IDmnFileAnalyzer dmnFileAnalyzer)
        {
            _dmnFileAnalyzer = dmnFileAnalyzer;
        }

        public async Task<DecisionContent> ExecuteAsync(string filePath, string decisionId, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"DMN file not found: {filePath}");
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                throw new ArgumentException("Decision ID cannot be null or empty", nameof(decisionId));
            }

            return await _dmnFileAnalyzer.ExtractDecisionAsync(filePath, decisionId, cancellationToken);
        }
    }
} 