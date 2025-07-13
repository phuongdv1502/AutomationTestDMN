namespace DmnTester.Presentation.Models
{
    public class EvaluateDecisionRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string DecisionId { get; set; } = string.Empty;
        public Dictionary<string, object> Inputs { get; set; } = new();
    }
} 