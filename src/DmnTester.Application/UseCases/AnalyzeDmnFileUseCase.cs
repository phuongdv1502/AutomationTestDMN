using DmnTester.Domain.Interfaces;
using DmnTester.Domain.Entities;

namespace DmnTester.Application.UseCases
{
    public class AnalyzeDmnFileUseCase
    {
        private readonly IDmnFileAnalyzer _dmnFileAnalyzer;

        public AnalyzeDmnFileUseCase(IDmnFileAnalyzer dmnFileAnalyzer)
        {
            _dmnFileAnalyzer = dmnFileAnalyzer;
        }

        public async Task<DmnFileInfo> ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"DMN file not found: {filePath}");
            }

            return await _dmnFileAnalyzer.AnalyzeDmnFileAsync(filePath, cancellationToken);
        }
    }
} 