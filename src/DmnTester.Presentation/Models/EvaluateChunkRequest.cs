namespace DmnTester.Presentation.Models
{
    public class EvaluateChunkRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string DecisionId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int ChunkSize { get; set; } = 50;
        public Dictionary<string, object> Inputs { get; set; } = new();
    }
} 