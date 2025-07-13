using System.Xml;
using System.Xml.Linq;
using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Infrastructure.Services
{
    public class DmnFileAnalyzer : IDmnFileAnalyzer
    {
        private const int DEFAULT_CHUNK_SIZE = 50;
        private const int LARGE_DECISION_THRESHOLD = 100; // Decision có >100 rules được coi là lớn

        public async Task<DmnFileInfo> AnalyzeDmnFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(filePath);
            var dmnFileInfo = new DmnFileInfo
            {
                FileName = fileInfo.Name,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime
            };

            try
            {
                using var stream = File.OpenRead(filePath);
                var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                
                var definitions = doc.Root;
                if (definitions?.Name.LocalName != "definitions")
                {
                    throw new InvalidOperationException("Invalid DMN file: root element must be 'definitions'");
                }

                dmnFileInfo.Namespace = definitions.Attribute("namespace")?.Value;
                dmnFileInfo.Exporter = definitions.Attribute("exporter")?.Value;
                dmnFileInfo.ExporterVersion = definitions.Attribute("exporterVersion")?.Value;

                var decisions = definitions.Elements().Where(e => e.Name.LocalName == "decision").ToList();
                dmnFileInfo.Decisions = new List<DecisionInfo>();

                foreach (var decision in decisions)
                {
                    var decisionInfo = new DecisionInfo
                    {
                        Id = decision.Attribute("id")?.Value,
                        Name = decision.Attribute("name")?.Value,
                        HistoryTimeToLive = decision.Attribute("{http://camunda.org/schema/1.0/dmn}historyTimeToLive")?.Value
                    };

                    // Analyze decision table
                    var decisionTable = decision.Elements().FirstOrDefault(e => e.Name.LocalName == "decisionTable");
                    if (decisionTable != null)
                    {
                        decisionInfo.HitPolicy = decisionTable.Attribute("hitPolicy")?.Value;
                        
                        // Count inputs
                        var inputs = decisionTable.Elements().Where(e => e.Name.LocalName == "input").ToList();
                        decisionInfo.InputCount = inputs.Count;
                        
                        // Count outputs
                        var outputs = decisionTable.Elements().Where(e => e.Name.LocalName == "output").ToList();
                        decisionInfo.OutputCount = outputs.Count;
                        
                        // Count rules
                        var rules = decisionTable.Elements().Where(e => e.Name.LocalName == "rule").ToList();
                        decisionInfo.RuleCount = rules.Count;
                        
                        // Estimate decision size
                        decisionInfo.EstimatedSize = EstimateDecisionSize(decision);

                        // Tính toán chunking
                        decisionInfo.NeedsChunking = decisionInfo.RuleCount > LARGE_DECISION_THRESHOLD;
                        decisionInfo.ChunkCount = decisionInfo.NeedsChunking ? 
                            (int)Math.Ceiling((double)decisionInfo.RuleCount / DEFAULT_CHUNK_SIZE) : 1;
                    }

                    dmnFileInfo.Decisions.Add(decisionInfo);
                }

                dmnFileInfo.TotalDecisions = dmnFileInfo.Decisions.Count;
                dmnFileInfo.TotalRules = dmnFileInfo.Decisions.Sum(d => d.RuleCount);
                dmnFileInfo.IsLargeFile = dmnFileInfo.TotalRules > 1000 || dmnFileInfo.FileSize > 1024 * 1024; // 1MB
            }
            catch (Exception ex)
            {
                dmnFileInfo.HasErrors = true;
                dmnFileInfo.ErrorMessage = ex.Message;
            }

            return dmnFileInfo;
        }

        public async Task<DecisionContent> ExtractDecisionAsync(string filePath, string decisionId, CancellationToken cancellationToken = default)
        {
            using var stream = File.OpenRead(filePath);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            
            var definitions = doc.Root;
            var decision = definitions?.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "decision" && e.Attribute("id")?.Value == decisionId);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision with id '{decisionId}' not found");
            }

            // Create a new DMN document with just this decision
            var newDefinitions = new XElement(definitions.Name, definitions.Attributes());
            newDefinitions.Add(decision);

            var newDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null), newDefinitions);
            
            return new DecisionContent
            {
                DecisionId = decisionId,
                DecisionName = decision.Attribute("name")?.Value,
                XmlContent = newDoc.ToString(),
                Size = newDoc.ToString().Length
            };
        }

        public async Task<List<DecisionSummary>> GetDecisionSummariesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var fileInfo = await AnalyzeDmnFileAsync(filePath, cancellationToken);
            return fileInfo.Decisions.Select(d => new DecisionSummary
            {
                Id = d.Id,
                Name = d.Name,
                RuleCount = d.RuleCount,
                InputCount = d.InputCount,
                OutputCount = d.OutputCount,
                EstimatedSize = d.EstimatedSize,
                IsLarge = d.RuleCount > LARGE_DECISION_THRESHOLD,
                ChunkCount = d.ChunkCount,
                NeedsChunking = d.NeedsChunking
            }).ToList();
        }

        public async Task<DecisionChunkInfo> GetDecisionChunksAsync(string filePath, string decisionId, int chunkSize = DEFAULT_CHUNK_SIZE, CancellationToken cancellationToken = default)
        {
            using var stream = File.OpenRead(filePath);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            
            var definitions = doc.Root;
            var decision = definitions?.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "decision" && e.Attribute("id")?.Value == decisionId);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision with id '{decisionId}' not found");
            }

            var decisionTable = decision.Elements().FirstOrDefault(e => e.Name.LocalName == "decisionTable");
            if (decisionTable == null)
            {
                throw new InvalidOperationException($"Decision table not found in decision '{decisionId}'");
            }

            var rules = decisionTable.Elements().Where(e => e.Name.LocalName == "rule").ToList();
            var totalRules = rules.Count;
            var totalChunks = (int)Math.Ceiling((double)totalRules / chunkSize);

            var chunkInfo = new DecisionChunkInfo
            {
                DecisionId = decisionId,
                DecisionName = decision.Attribute("name")?.Value,
                TotalRules = totalRules,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks,
                Chunks = new List<DecisionChunk>()
            };

            // Tạo các chunks
            for (int i = 0; i < totalChunks; i++)
            {
                var startIndex = i * chunkSize;
                var endIndex = Math.Min(startIndex + chunkSize - 1, totalRules - 1);
                var chunkRules = rules.Skip(startIndex).Take(chunkSize).ToList();

                var chunkDecision = CreateChunkDecision(decision, decisionTable, chunkRules, i);
                var chunkXml = CreateChunkXml(definitions, chunkDecision);

                var chunk = new DecisionChunk
                {
                    DecisionId = decisionId,
                    DecisionName = decision.Attribute("name")?.Value,
                    ChunkIndex = i,
                    StartRuleIndex = startIndex,
                    EndRuleIndex = endIndex,
                    RuleCount = chunkRules.Count,
                    XmlContent = chunkXml,
                    Size = chunkXml.Length,
                    IsLastChunk = i == totalChunks - 1
                };

                chunkInfo.Chunks.Add(chunk);
            }

            return chunkInfo;
        }

        public async Task<DecisionChunk> GetDecisionChunkAsync(string filePath, string decisionId, int chunkIndex, int chunkSize = DEFAULT_CHUNK_SIZE, CancellationToken cancellationToken = default)
        {
            var chunkInfo = await GetDecisionChunksAsync(filePath, decisionId, chunkSize, cancellationToken);
            
            if (chunkIndex < 0 || chunkIndex >= chunkInfo.TotalChunks)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkIndex), $"Chunk index {chunkIndex} is out of range. Total chunks: {chunkInfo.TotalChunks}");
            }

            return chunkInfo.Chunks[chunkIndex];
        }

        public async Task<List<DecisionChunk>> GetAllDecisionChunksAsync(string filePath, string decisionId, int chunkSize = DEFAULT_CHUNK_SIZE, CancellationToken cancellationToken = default)
        {
            var chunkInfo = await GetDecisionChunksAsync(filePath, decisionId, chunkSize, cancellationToken);
            return chunkInfo.Chunks;
        }

        private XElement CreateChunkDecision(XElement originalDecision, XElement originalTable, List<XElement> chunkRules, int chunkIndex)
        {
            // Tạo decision mới với suffix chunk index
            var chunkDecision = new XElement(originalDecision.Name, originalDecision.Attributes());
            
            // Thêm suffix vào ID và name để phân biệt
            var originalId = chunkDecision.Attribute("id")?.Value;
            var originalName = chunkDecision.Attribute("name")?.Value;
            
            chunkDecision.SetAttributeValue("id", $"{originalId}_chunk_{chunkIndex}");
            chunkDecision.SetAttributeValue("name", $"{originalName} (Chunk {chunkIndex + 1})");

            // Tạo decision table mới với chỉ các rules trong chunk
            var chunkTable = new XElement(originalTable.Name, originalTable.Attributes());
            
            // Copy inputs và outputs
            var inputs = originalTable.Elements().Where(e => e.Name.LocalName == "input");
            var outputs = originalTable.Elements().Where(e => e.Name.LocalName == "output");
            
            foreach (var input in inputs)
            {
                chunkTable.Add(new XElement(input));
            }
            
            foreach (var output in outputs)
            {
                chunkTable.Add(new XElement(output));
            }
            
            // Thêm rules của chunk
            foreach (var rule in chunkRules)
            {
                chunkTable.Add(new XElement(rule));
            }

            chunkDecision.Add(chunkTable);
            return chunkDecision;
        }

        private string CreateChunkXml(XElement definitions, XElement chunkDecision)
        {
            var newDefinitions = new XElement(definitions.Name, definitions.Attributes());
            newDefinitions.Add(chunkDecision);
            var newDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null), newDefinitions);
            return newDoc.ToString();
        }

        private long EstimateDecisionSize(XElement decision)
        {
            return decision.ToString().Length;
        }
    }
} 