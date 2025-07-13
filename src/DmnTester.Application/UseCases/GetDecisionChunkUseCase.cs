using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Application.UseCases
{
    public class GetDecisionChunkUseCase
    {
        private readonly IDmnFileAnalyzer _dmnFileAnalyzer;

        public GetDecisionChunkUseCase(IDmnFileAnalyzer dmnFileAnalyzer)
        {
            _dmnFileAnalyzer = dmnFileAnalyzer;
        }

        public async Task<DecisionChunk> ExecuteAsync(string filePath, string decisionId, int chunkIndex, int chunkSize = 50, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"DMN file not found: {filePath}");
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                throw new ArgumentException("Decision ID cannot be null or empty", nameof(decisionId));
            }

            if (chunkIndex < 0)
            {
                throw new ArgumentException("Chunk index must be non-negative", nameof(chunkIndex));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));
            }

            return await _dmnFileAnalyzer.GetDecisionChunkAsync(filePath, decisionId, chunkIndex, chunkSize, cancellationToken);
        }
    }
} 