namespace DmnTester.Domain.Entities
{
    public class DmnFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string? Namespace { get; set; }
        public string? Exporter { get; set; }
        public string? ExporterVersion { get; set; }
        public List<DecisionInfo> Decisions { get; set; } = new();
        public int TotalDecisions { get; set; }
        public int TotalRules { get; set; }
        public bool IsLargeFile { get; set; }
        public bool HasErrors { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DecisionInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? HistoryTimeToLive { get; set; }
        public string? HitPolicy { get; set; }
        public int InputCount { get; set; }
        public int OutputCount { get; set; }
        public int RuleCount { get; set; }
        public long EstimatedSize { get; set; }
        public int ChunkCount { get; set; } // Số chunk cần chia
        public bool NeedsChunking { get; set; } // Có cần chia nhỏ không
    }

    public class DecisionContent
    {
        public string DecisionId { get; set; } = string.Empty;
        public string? DecisionName { get; set; }
        public string XmlContent { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public class DecisionSummary
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int RuleCount { get; set; }
        public int InputCount { get; set; }
        public int OutputCount { get; set; }
        public long EstimatedSize { get; set; }
        public bool IsLarge { get; set; }
        public int ChunkCount { get; set; }
        public bool NeedsChunking { get; set; }
    }

    // Thêm entities mới cho rule chunking
    public class DecisionChunk
    {
        public string DecisionId { get; set; } = string.Empty;
        public string? DecisionName { get; set; }
        public int ChunkIndex { get; set; }
        public int StartRuleIndex { get; set; }
        public int EndRuleIndex { get; set; }
        public int RuleCount { get; set; }
        public string XmlContent { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsLastChunk { get; set; }
    }

    public class DecisionChunkInfo
    {
        public string DecisionId { get; set; } = string.Empty;
        public string? DecisionName { get; set; }
        public int TotalRules { get; set; }
        public int ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public List<DecisionChunk> Chunks { get; set; } = new();
    }
} 