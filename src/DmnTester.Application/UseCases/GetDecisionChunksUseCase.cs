using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Application.UseCases
{
    public class GetDecisionChunksUseCase
    {
        private readonly IDmnFileAnalyzer _dmnFileAnalyzer;

        public GetDecisionChunksUseCase(IDmnFileAnalyzer dmnFileAnalyzer)
        {
            _dmnFileAnalyzer = dmnFileAnalyzer;
        }

        public async Task<DecisionChunkInfo> ExecuteAsync(string filePath, string decisionId, int chunkSize = 50, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"DMN file not found: {filePath}");
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                throw new ArgumentException("Decision ID cannot be null or empty", nameof(decisionId));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));
            }

            return await _dmnFileAnalyzer.GetDecisionChunksAsync(filePath, decisionId, chunkSize, cancellationToken);
        }
    }
} 