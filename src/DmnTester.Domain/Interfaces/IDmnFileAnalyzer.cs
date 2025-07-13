using DmnTester.Domain.Entities;

namespace DmnTester.Domain.Interfaces
{
    public interface IDmnFileAnalyzer
    {
        Task<DmnFileInfo> AnalyzeDmnFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<DecisionContent> ExtractDecisionAsync(string filePath, string decisionId, CancellationToken cancellationToken = default);
        Task<List<DecisionSummary>> GetDecisionSummariesAsync(string filePath, CancellationToken cancellationToken = default);
        
        // Thêm methods mới cho rule chunking
        Task<DecisionChunkInfo> GetDecisionChunksAsync(string filePath, string decisionId, int chunkSize = 50, CancellationToken cancellationToken = default);
        Task<DecisionChunk> GetDecisionChunkAsync(string filePath, string decisionId, int chunkIndex, int chunkSize = 50, CancellationToken cancellationToken = default);
        Task<List<DecisionChunk>> GetAllDecisionChunksAsync(string filePath, string decisionId, int chunkSize = 50, CancellationToken cancellationToken = default);
    }
} 