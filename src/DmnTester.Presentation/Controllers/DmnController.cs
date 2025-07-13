using Microsoft.AspNetCore.Mvc;
using DmnTester.Presentation.Models;
using DmnTester.Application.UseCases;
using System.Text.Json;

namespace DmnTester.Presentation.Controllers;

[ApiController]
[Route("api/dmn")]
public class DmnController : ControllerBase
{
    private readonly GetConfigsUseCase _getConfigsUseCase;
    private readonly GetConfigUseCase _getConfigUseCase;
    private readonly EvaluateDmnUseCase _evaluateDmnUseCase;
    private readonly TestDmnUseCase _testDmnUseCase;
    private readonly GenerateTestFromDmnUseCase _generateTestFromDmnUseCase;
    private readonly AnalyzeDmnFileUseCase _analyzeDmnFileUseCase;
    private readonly ExtractDecisionUseCase _extractDecisionUseCase;
    private readonly GetDecisionSummariesUseCase _getDecisionSummariesUseCase;
    private readonly GetDecisionChunksUseCase _getDecisionChunksUseCase;
    private readonly GetDecisionChunkUseCase _getDecisionChunkUseCase;
    private readonly IConfiguration _configuration;
    private readonly TimeoutSettings _timeoutSettings;

    public DmnController(
        GetConfigsUseCase getConfigsUseCase,
        GetConfigUseCase getConfigUseCase,
        EvaluateDmnUseCase evaluateDmnUseCase,
        TestDmnUseCase testDmnUseCase,
        GenerateTestFromDmnUseCase generateTestFromDmnUseCase,
        AnalyzeDmnFileUseCase analyzeDmnFileUseCase,
        ExtractDecisionUseCase extractDecisionUseCase,
        GetDecisionSummariesUseCase getDecisionSummariesUseCase,
        GetDecisionChunksUseCase getDecisionChunksUseCase,
        GetDecisionChunkUseCase getDecisionChunkUseCase,
        IConfiguration configuration,
        TimeoutSettings timeoutSettings)
    {
        _getConfigsUseCase = getConfigsUseCase;
        _getConfigUseCase = getConfigUseCase;
        _evaluateDmnUseCase = evaluateDmnUseCase;
        _testDmnUseCase = testDmnUseCase;
        _generateTestFromDmnUseCase = generateTestFromDmnUseCase;
        _analyzeDmnFileUseCase = analyzeDmnFileUseCase;
        _extractDecisionUseCase = extractDecisionUseCase;
        _getDecisionSummariesUseCase = getDecisionSummariesUseCase;
        _getDecisionChunksUseCase = getDecisionChunksUseCase;
        _getDecisionChunkUseCase = getDecisionChunkUseCase;
        _configuration = configuration;
        _timeoutSettings = timeoutSettings;
    }

    [HttpGet("configs")]
    public async Task<IActionResult> GetConfigs()
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var configPath = _configuration["DmnConfigPath"] ?? "demo/dmnConfigs";
            var absoluteConfigPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
            var configs = await _getConfigsUseCase.ExecuteAsync(absoluteConfigPath);
            return Ok(configs);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Request timed out after {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("configs/{decisionId}")]
    public async Task<IActionResult> GetConfig(string decisionId)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var configPath = _configuration["DmnConfigPath"] ?? "demo/dmnConfigs";
            var absoluteConfigPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
            var configFile = Path.Combine(absoluteConfigPath, $"{decisionId}.json");
            var config = await _getConfigUseCase.ExecuteAsync(configFile);
            return Ok(config);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Request timed out after {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.DmnEvaluationTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "demo/dmns";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.DmnPath);
            var result = await _evaluateDmnUseCase.ExecuteAsync(dmnPath, request.DecisionId, request.Inputs);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"DMN evaluation timed out after {_timeoutSettings.DmnEvaluationTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test([FromBody] TestRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.DmnEvaluationTimeout);
        try
        {
            var configPath = _configuration["DmnConfigPath"] ?? "demo/dmnConfigs";
            var absoluteConfigPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
            var configFile = Path.Combine(absoluteConfigPath, $"{request.DecisionId}.json");
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "demo/dmns";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, $"{request.DecisionId}.dmn");
            var result = await _testDmnUseCase.ExecuteAsync(request.DecisionId, configFile, dmnPath);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Test execution timed out after {_timeoutSettings.DmnEvaluationTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class GenerateTestRequestWithPaging : GenerateTestRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public bool UseStreaming { get; set; } = false; // Thêm option để dùng streaming
    }

    [HttpPost("generate-test")]
    public async Task<IActionResult> GenerateTestFromDmn([FromBody] GenerateTestRequestWithPaging request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.DmnFileName);
            
            // Kiểm tra file có tồn tại không
            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {request.DmnFileName} không tồn tại" });
            }

            // Nếu dùng streaming, sử dụng phương pháp mới
            if (request.UseStreaming)
            {
                return await GenerateTestWithStreaming(dmnPath, request, cts.Token);
            }

            // Phương pháp cũ (để backward compatibility)
            return await GenerateTestLegacy(dmnPath, request, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Test generation timed out after {_timeoutSettings.RequestTimeout}ms. File có thể quá lớn." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error generating test cases: {ex.Message}" });
        }
    }

    private async Task<IActionResult> GenerateTestWithStreaming(string dmnPath, GenerateTestRequestWithPaging request, CancellationToken cancellationToken)
    {
        // Đếm rules bằng streaming (không load toàn bộ file)
        var ruleCount = await CountRulesWithStreaming(dmnPath, cancellationToken);
        
        // Nếu file quá lớn, cảnh báo user
        if (ruleCount > 1000)
        {
            return BadRequest(new { 
                error = $"File DMN quá lớn với {ruleCount} rules. Vui lòng sử dụng file nhỏ hơn hoặc chia nhỏ file.",
                ruleCount = ruleCount,
                suggestion = "Có thể chia file thành nhiều decision table nhỏ hơn"
            });
        }

        // Tính toán pagination
        int page = request.Page > 0 ? request.Page : 1;
        int pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 100, 200);
        int skip = (page - 1) * pageSize;

        // Generate test cases theo chunk
        var testCases = await GenerateTestCasesInChunks(dmnPath, skip, pageSize, cancellationToken);
        
        // Ước tính tổng số test cases (có thể không chính xác 100% nhưng đủ để pagination)
        var estimatedTotal = ruleCount * 2; // Ước tính trung bình 2 test cases per rule

        return Ok(new { 
            total = estimatedTotal, 
            page, 
            pageSize, 
            data = testCases,
            ruleCount = ruleCount,
            estimatedTestCases = estimatedTotal,
            warning = estimatedTotal > 500 ? $"File này sẽ tạo ra khoảng {estimatedTotal} test cases. Có thể mất thời gian để xử lý." : null,
            streaming = true
        });
    }

    private async Task<IActionResult> GenerateTestLegacy(string dmnPath, GenerateTestRequestWithPaging request, CancellationToken cancellationToken)
    {
        // Đọc file DMN để ước tính số lượng rules
        var dmnContent = System.IO.File.ReadAllText(dmnPath);
        var ruleCount = System.Text.RegularExpressions.Regex.Matches(dmnContent, @"<rule id=""[^""]+""").Count;
        
        // Nếu file quá lớn, cảnh báo user
        if (ruleCount > 1000)
        {
            return BadRequest(new { 
                error = $"File DMN quá lớn với {ruleCount} rules. Vui lòng sử dụng file nhỏ hơn hoặc chia nhỏ file.",
                ruleCount = ruleCount,
                suggestion = "Có thể chia file thành nhiều decision table nhỏ hơn"
            });
        }

        var configs = await _generateTestFromDmnUseCase.ExecuteAllAsync(dmnPath);
        var result = configs.Select(x => new { decisionId = x.decisionId, testConfig = x.testConfig }).ToList();
        
        // Paging
        int total = result.Count;
        int page = request.Page > 0 ? request.Page : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 100;
        
        // Giới hạn pageSize để tránh quá tải
        pageSize = Math.Min(pageSize, 200);
        
        var paged = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        return Ok(new { 
            total, 
            page, 
            pageSize, 
            data = paged,
            ruleCount = ruleCount,
            estimatedTestCases = total,
            warning = total > 500 ? $"File này sẽ tạo ra {total} test cases. Có thể mất thời gian để xử lý." : null,
            streaming = false
        });
    }

    private async Task<int> CountRulesWithStreaming(string filePath, CancellationToken cancellationToken)
    {
        var ruleCount = 0;
        using var reader = new StreamReader(filePath);
        string? line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Contains("<rule id=\""))
            {
                ruleCount++;
            }
        }
        
        return ruleCount;
    }

    private async Task<List<object>> GenerateTestCasesInChunks(string dmnPath, int skip, int take, CancellationToken cancellationToken)
    {
        // Sử dụng streaming thay vì load toàn bộ file
        return await GenerateTestCasesWithStreaming(dmnPath, skip, take, cancellationToken);
    }

    private async Task<List<object>> GenerateTestCasesForChunk(string dmnPath, int startRule, int endRule, CancellationToken cancellationToken)
    {
        // Sử dụng streaming thay vì load toàn bộ file
        return await GenerateTestCasesWithStreaming(dmnPath, startRule, endRule - startRule, cancellationToken);
    }

    private async Task<List<object>> GenerateTestCasesWithStreaming(string dmnPath, int skip, int take, CancellationToken cancellationToken)
    {
        var testCases = new List<object>();
        var currentRuleIndex = 0;
        var rulesToProcess = new List<string>();
        var currentDecisionId = "";
        var inDecisionTable = false;
        var inRule = false;
        var currentRuleXml = "";
        
        using var reader = new StreamReader(dmnPath);
        string? line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Tìm decision table
            if (line.Contains("<decisionTable"))
            {
                inDecisionTable = true;
                // Tìm decision ID từ các dòng trước đó hoặc trong decision table
                if (string.IsNullOrEmpty(currentDecisionId))
                {
                    // Tìm decision ID từ context
                    var decisionMatch = System.Text.RegularExpressions.Regex.Match(line, @"decision id=""([^""]+)""");
                    if (decisionMatch.Success)
                    {
                        currentDecisionId = decisionMatch.Groups[1].Value;
                    }
                }
            }
            
            // Tìm rule
            if (inDecisionTable && line.Contains("<rule"))
            {
                inRule = true;
                currentRuleXml = line;
                
                // Nếu đã đủ rules cần xử lý, dừng lại
                if (currentRuleIndex >= skip + take)
                {
                    break;
                }
                
                // Nếu rule này nằm trong range cần xử lý
                if (currentRuleIndex >= skip)
                {
                    rulesToProcess.Add(currentRuleXml);
                }
                
                currentRuleIndex++;
            }
            else if (inRule)
            {
                // Tiếp tục đọc rule XML
                currentRuleXml += "\n" + line;
                
                // Nếu kết thúc rule
                if (line.Contains("</rule>"))
                {
                    inRule = false;
                    
                    // Cập nhật rule cuối cùng nếu đang trong range
                    if (currentRuleIndex > skip && currentRuleIndex <= skip + take)
                    {
                        rulesToProcess[rulesToProcess.Count - 1] = currentRuleXml;
                    }
                }
            }
            
            // Kết thúc decision table
            if (line.Contains("</decisionTable>"))
            {
                inDecisionTable = false;
                break; // Chỉ xử lý decision table đầu tiên
            }
        }
        
        // Tạo test cases từ rules đã thu thập
        if (rulesToProcess.Count > 0 && !string.IsNullOrEmpty(currentDecisionId))
        {
            var testConfig = CreateTestConfigFromRules(rulesToProcess, currentDecisionId);
            testCases.Add(new { decisionId = currentDecisionId, testConfig });
        }
        
        return testCases;
    }

    private object CreateTestConfigFromRules(List<string> rules, string decisionId)
    {
        // Tạo test config đơn giản từ rules
        var testCases = new List<object>();
        
        foreach (var ruleXml in rules)
        {
            // Parse rule XML để tạo test case
            var testCase = ParseRuleToTestCase(ruleXml);
            if (testCase != null)
            {
                testCases.Add(testCase);
            }
        }
        
        return new
        {
            dmnPath = new[] { $"{decisionId}.dmn" },
            data = new
            {
                testCases = testCases
            }
        };
    }

    private object? ParseRuleToTestCase(string ruleXml)
    {
        try
        {
            // Parse rule XML để lấy inputs và outputs
            var inputs = new Dictionary<string, object>();
            var outputs = new Dictionary<string, object>();
            
            // Tìm input entries
            var inputMatches = System.Text.RegularExpressions.Regex.Matches(ruleXml, @"<inputEntry id=""[^""]*"">([^<]+)</inputEntry>");
            foreach (System.Text.RegularExpressions.Match match in inputMatches)
            {
                var inputValue = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(inputValue) && inputValue != "\"\"")
                {
                    inputs[$"input_{inputs.Count}"] = ParseValue(inputValue);
                }
            }
            
            // Tìm output entries
            var outputMatches = System.Text.RegularExpressions.Regex.Matches(ruleXml, @"<outputEntry id=""[^""]*"">([^<]+)</outputEntry>");
            foreach (System.Text.RegularExpressions.Match match in outputMatches)
            {
                var outputValue = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(outputValue) && outputValue != "\"\"")
                {
                    outputs[$"output_{outputs.Count}"] = ParseValue(outputValue);
                }
            }
            
            if (inputs.Count > 0 || outputs.Count > 0)
            {
                return new
                {
                    name = $"Rule_{Guid.NewGuid().ToString("N")[..8]}",
                    inputs = inputs,
                    expectedOutputs = outputs
                };
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return null;
    }

    private object ParseValue(string value)
    {
        // Parse các loại giá trị khác nhau
        value = value.Trim('"');
        
        if (int.TryParse(value, out var intValue))
            return intValue;
        if (double.TryParse(value, out var doubleValue))
            return doubleValue;
        if (bool.TryParse(value, out var boolValue))
            return boolValue;
        
        return value;
    }

    private object NormalizeActual(object actualValue)
    {
        if (actualValue is System.Collections.IList list)
        {
            if (list.Count == 1)
                return list[0];
            if (list.Count > 1)
                return actualValue; // Trả nguyên list nếu có nhiều hơn 1 phần tử
        }
        return actualValue;
    }

    private Dictionary<string, object> RemapActualOutputs(Dictionary<string, object> actual, List<string> outputKeys)
    {
        // Nếu actual chỉ có 1 key và value là list hỗn hợp, tách ra từng key
        if (actual.Count == 1 && actual.Values.First() is System.Collections.IList list)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < outputKeys.Count; i++)
            {
                var values = new List<object>();
                for (int j = i; j < list.Count; j += outputKeys.Count)
                {
                    values.Add(list[j]);
                }
                // Nếu chỉ có 1 giá trị, trả về giá trị đơn lẻ
                result[outputKeys[i]] = values.Count == 1 ? values[0] : values;
            }
            return result;
        }
        return actual;
    }

    [HttpPost("batch-test")]
    public async Task<IActionResult> BatchTest([FromBody] BatchTestRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.BatchTestTimeout);
        try
        {
            var allResults = new List<object>();
            foreach (var item in request.Items)
            {
                cts.Token.ThrowIfCancellationRequested();
                var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
                var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
                var dmnPath = Path.Combine(absoluteDmnPath, item.DmnFileName);
                var results = new List<object>();
                // Lấy outputKeys từ test case đầu tiên (giả định các test case cùng 1 decision)
                var outputKeys = item.TestCases.FirstOrDefault()?.ExpectedOutputs.Keys.ToList() ?? new List<string>();
                foreach (var testCase in item.TestCases)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    var evalResult = await _evaluateDmnUseCase.ExecuteAsync(dmnPath, item.DecisionId, testCase.Inputs);
                    // Normalize actualOutputs
                    var normalizedActualOutputs = evalResult.Outputs.ToDictionary(
                        kv => kv.Key,
                        kv => NormalizeActual(kv.Value)
                    );
                    // Remap nếu bị trả về dạng mảng hỗn hợp
                    var remappedActualOutputs = RemapActualOutputs(normalizedActualOutputs, outputKeys);
                    // Đảm bảo đủ key như expectedOutputs
                    var allKeys = testCase.ExpectedOutputs.Keys.Union(remappedActualOutputs.Keys).ToList();
                    // Normalize actualOutputs khi trả về: nếu expected là đơn và actual là list, lấy phần tử đầu tiên
                    var completedActualOutputs = allKeys.ToDictionary(
                        key => key,
                        key => {
                            var value = remappedActualOutputs.ContainsKey(key) ? remappedActualOutputs[key] : null;
                            if (!(testCase.ExpectedOutputs.ContainsKey(key) && testCase.ExpectedOutputs[key] is System.Collections.IList) && value is System.Collections.IList list && list.Count > 0)
                                return list[0];
                            return value;
                        }
                    );
                    var pass = CompareOutputs(completedActualOutputs, testCase.ExpectedOutputs);
                    var resultItem = new
                    {
                        name = testCase.Name,
                        pass,
                        actualOutputs = completedActualOutputs,
                        expectedOutputs = testCase.ExpectedOutputs,
                        diff = GetDiff(completedActualOutputs, testCase.ExpectedOutputs),
                        inputs = testCase.Inputs
                    };
                    results.Add(resultItem);
                }
                allResults.Add(new
                {
                    decisionId = item.DecisionId,
                    dmnFileName = item.DmnFileName,
                    results
                });
            }
            return Ok(allResults);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Batch test timed out after {_timeoutSettings.BatchTestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("convert-generate-to-batch")]
    public IActionResult ConvertGenerateToBatch([FromBody] ConvertGenerateToBatchRequest request)
    {
        var batch = new BatchTestRequest
        {
            Items = request.Items.Select(x => new BatchTestItem
            {
                DmnFileName = x.TestConfig.DmnPath.FirstOrDefault() ?? string.Empty,
                DecisionId = x.DecisionId,
                TestCases = x.TestConfig.Data.TestCases
            }).ToList()
        };
        return Ok(batch);
    }

    private bool DeepEquals(object a, object b)
    {
        var options = new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = null };
        var jsonA = JsonSerializer.Serialize(a, options);
        var jsonB = JsonSerializer.Serialize(b, options);
        return jsonA == jsonB;
    }

    private bool CompareOutputs(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        if (expected.Count == 0)
        {
            return actual.Count > 0;
        }
        var allKeys = expected.Keys.Union(actual.Keys).ToList();
        foreach (var key in allKeys)
        {
            expected.TryGetValue(key, out var expectedValue);
            actual.TryGetValue(key, out var actualValue);
            actualValue = NormalizeActual(actualValue);

            // Nếu actualValue là list và expectedValue không phải list, lấy phần tử đầu tiên
            if (actualValue is System.Collections.IList list && !(expectedValue is System.Collections.IList))
            {
                if (list.Count == 0) return false;
                actualValue = list[0];
            }

            if (expectedValue == null && actualValue == null)
                continue;
            if (expectedValue is List<string> expectedStringList)
            {
                if (!(actualValue is List<string> actualStringList) || !expectedStringList.SequenceEqual(actualStringList))
                    return false;
            }
            else if (actualValue is List<string> actualStringList2)
            {
                if (!actualStringList2.Contains(expectedValue?.ToString()))
                    return false;
            }
            else
            {
                if (!DeepEquals(actualValue, expectedValue))
                    return false;
            }
        }
        return true;
    }

    private object GetDiff(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        var diff = new Dictionary<string, object>();
        var allKeys = expected.Keys.Union(actual.Keys).ToList();
        foreach (var key in allKeys)
        {
            expected.TryGetValue(key, out var expectedValue);
            actual.TryGetValue(key, out var actualValue);
            actualValue = NormalizeActual(actualValue);

            // Nếu actualValue là list và expectedValue không phải list, lấy phần tử đầu tiên
            if (actualValue is System.Collections.IList list && !(expectedValue is System.Collections.IList))
            {
                if (list.Count == 0)
                {
                    diff[key] = new { expected = expectedValue, actual = actualValue };
                    continue;
                }
                actualValue = list[0];
            }

            if (expectedValue == null && actualValue == null)
                continue;
            if (expectedValue is List<string> expectedStringList)
            {
                if (!(actualValue is List<string> actualStringList) || !expectedStringList.SequenceEqual(actualStringList))
                {
                    diff[key] = new { expected = expectedValue, actual = actualValue };
                }
            }
            else if (actualValue is List<string> actualStringList2)
            {
                if (!actualStringList2.Contains(expectedValue?.ToString()))
                {
                    diff[key] = new { expected = expectedValue, actual = actualValue };
                }
            }
            else
            {
                if (actualValue?.ToString() != expectedValue?.ToString())
                {
                    diff[key] = new { expected = expectedValue, actual = actualValue };
                }
            }
        }
        return diff;
    }

    [HttpGet("read-file")]
    public IActionResult ReadDmnFile([FromQuery] string fileName, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            var allLines = System.IO.File.ReadAllLines(dmnPath);
            var totalLines = allLines.Length;
            var totalPages = (int)Math.Ceiling((double)totalLines / pageSize);
            
            // Đảm bảo page hợp lệ
            page = Math.Max(1, Math.Min(page, totalPages));
            pageSize = Math.Max(1, Math.Min(pageSize, 1000)); // Giới hạn tối đa 1000 dòng mỗi trang

            var startIndex = (page - 1) * pageSize;
            var endIndex = Math.Min(startIndex + pageSize, totalLines);
            var lines = allLines.Skip(startIndex).Take(endIndex - startIndex).ToArray();

            return Ok(new
            {
                fileName,
                totalLines,
                totalPages,
                currentPage = page,
                pageSize,
                startLine = startIndex + 1,
                endLine = endIndex,
                lines,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi đọc file: {ex.Message}" });
        }
    }

    [HttpGet("list-files")]
    public IActionResult ListDmnFiles()
    {
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);

            if (!Directory.Exists(absoluteDmnPath))
            {
                return NotFound(new { error = "Thư mục DMN không tồn tại" });
            }

            var files = Directory.GetFiles(absoluteDmnPath, "*.dmn")
                .Select(filePath => new
                {
                    fileName = Path.GetFileName(filePath),
                    fileSize = new System.IO.FileInfo(filePath).Length,
                    lastModified = System.IO.File.GetLastWriteTime(filePath),
                    lineCount = System.IO.File.ReadAllLines(filePath).Length
                })
                .OrderBy(f => f.fileName)
                .ToList();

            return Ok(new { files });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi liệt kê files: {ex.Message}" });
        }
    }

    [HttpGet("analyze-dmn")]
    public IActionResult AnalyzeDmnFile([FromQuery] string fileName)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 giây timeout cho phân tích
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            var fileInfo = new System.IO.FileInfo(dmnPath);
            
            // Đếm số dòng hiệu quả hơn
            var lineCount = CountLinesEfficiently(dmnPath);
            cts.Token.ThrowIfCancellationRequested();
            
            // Nếu file quá lớn (>10MB), chỉ đọc phần đầu để phân tích nhanh
            var maxReadSize = 10 * 1024 * 1024; // 10MB
            var shouldReadFull = fileInfo.Length <= maxReadSize;
            
            string dmnContent;
            if (shouldReadFull)
            {
                dmnContent = System.IO.File.ReadAllText(dmnPath);
            }
            else
            {
                // Đọc từng chunk để phân tích nhanh
                dmnContent = ReadFileInChunks(dmnPath, maxReadSize);
            }
            
            // Phân tích file DMN
            cts.Token.ThrowIfCancellationRequested();
            var ruleCount = System.Text.RegularExpressions.Regex.Matches(dmnContent, @"<rule id=""[^""]+""").Count;
            var decisionCount = System.Text.RegularExpressions.Regex.Matches(dmnContent, @"<decision id=""[^""]+""").Count;
            var tableCount = System.Text.RegularExpressions.Regex.Matches(dmnContent, @"<decisionTable id=""[^""]+""").Count;
            
            // Tìm tên các decision (chỉ lấy 10 đầu tiên nếu có nhiều)
            var decisionMatches = System.Text.RegularExpressions.Regex.Matches(dmnContent, @"<decision id=""([^""]+)""[^>]*name=""([^""]+)""");
            var decisions = decisionMatches.Cast<System.Text.RegularExpressions.Match>()
                .Take(10) // Giới hạn 10 decisions để tránh response quá lớn
                .Select(m => new { id = m.Groups[1].Value, name = m.Groups[2].Value })
                .ToList();

            // Ước tính rule count nếu file quá lớn
            if (!shouldReadFull && ruleCount > 0)
            {
                var sampleRatio = (double)maxReadSize / fileInfo.Length;
                ruleCount = (int)(ruleCount / sampleRatio);
            }
            
            return Ok(new
            {
                fileName,
                fileSize = fileInfo.Length,
                lineCount,
                ruleCount,
                decisionCount,
                tableCount,
                decisions,
                estimatedTestCases = ruleCount * 2, // Ước tính số test cases
                complexity = ruleCount > 1000 ? "High" : ruleCount > 500 ? "Medium" : "Low",
                recommendation = ruleCount > 1000 ? 
                    "File quá lớn. Nên chia nhỏ thành nhiều decision table." : 
                    ruleCount > 500 ? 
                    "File khá lớn. Có thể mất thời gian để generate test cases." : 
                    "File có kích thước phù hợp.",
                isLargeFile = !shouldReadFull,
                note = !shouldReadFull ? "Phân tích dựa trên mẫu file (do file quá lớn)" : null
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = "Phân tích file timeout sau 10 giây. File có thể quá lớn." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi phân tích file: {ex.Message}" });
        }
    }

    private string ReadFileInChunks(string filePath, int maxSize)
    {
        var content = new System.Text.StringBuilder();
        var buffer = new char[8192]; // 8KB buffer
        var totalRead = 0;
        
        using (var reader = new System.IO.StreamReader(filePath))
        {
            int bytesRead;
            while (totalRead < maxSize && (bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                content.Append(buffer, 0, bytesRead);
                totalRead += bytesRead;
            }
        }
        
        return content.ToString();
    }

    private int CountLinesEfficiently(string filePath)
    {
        var lineCount = 0;
        var buffer = new char[8192]; // 8KB buffer
        var lineBreakCount = 0;
        
        using (var reader = new System.IO.StreamReader(filePath))
        {
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        lineBreakCount++;
                    }
                }
            }
        }
        
        return lineBreakCount + 1; // +1 vì dòng cuối có thể không có \n
    }

    [HttpGet("analyze-file")]
    public async Task<IActionResult> AnalyzeDmnFileDetailed([FromQuery] string fileName)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            var fileInfo = await _analyzeDmnFileUseCase.ExecuteAsync(dmnPath, cts.Token);
            return Ok(fileInfo);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Phân tích file timeout sau {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi phân tích file: {ex.Message}" });
        }
    }

    [HttpGet("decision-summaries")]
    public async Task<IActionResult> GetDecisionSummaries([FromQuery] string fileName)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            var summaries = await _getDecisionSummariesUseCase.ExecuteAsync(dmnPath, cts.Token);
            return Ok(new { fileName, decisions = summaries });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Lấy danh sách decisions timeout sau {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi lấy danh sách decisions: {ex.Message}" });
        }
    }

    [HttpGet("extract-decision")]
    public async Task<IActionResult> ExtractDecision([FromQuery] string fileName, [FromQuery] string decisionId)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                return BadRequest(new { error = "Decision ID không được để trống" });
            }

            var decisionContent = await _extractDecisionUseCase.ExecuteAsync(dmnPath, decisionId, cts.Token);
            return Ok(decisionContent);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Trích xuất decision timeout sau {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi trích xuất decision: {ex.Message}" });
        }
    }

    [HttpPost("evaluate-decision")]
    public async Task<IActionResult> EvaluateDecision([FromBody] EvaluateDecisionRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.DmnEvaluationTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.FileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {request.FileName} không tồn tại" });
            }

            // Trích xuất decision
            var decisionContent = await _extractDecisionUseCase.ExecuteAsync(dmnPath, request.DecisionId, cts.Token);
            
            // Tạo file tạm thời cho decision
            var tempDir = Path.GetTempPath();
            var tempFileName = $"temp_decision_{Guid.NewGuid()}.dmn";
            var tempFilePath = Path.Combine(tempDir, tempFileName);
            
            try
            {
                await System.IO.File.WriteAllTextAsync(tempFilePath, decisionContent.XmlContent, cts.Token);
                
                // Đánh giá decision
                var result = await _evaluateDmnUseCase.ExecuteAsync(tempFilePath, request.DecisionId, request.Inputs);
                return Ok(result);
            }
            finally
            {
                // Xóa file tạm thời
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Đánh giá decision timeout sau {_timeoutSettings.DmnEvaluationTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi đánh giá decision: {ex.Message}" });
        }
    }

    [HttpGet("decision-chunks")]
    public async Task<IActionResult> GetDecisionChunks([FromQuery] string fileName, [FromQuery] string decisionId, [FromQuery] int chunkSize = 50)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                return BadRequest(new { error = "Decision ID không được để trống" });
            }

            if (chunkSize <= 0 || chunkSize > 200)
            {
                return BadRequest(new { error = "Chunk size phải từ 1 đến 200" });
            }

            var chunkInfo = await _getDecisionChunksUseCase.ExecuteAsync(dmnPath, decisionId, chunkSize, cts.Token);
            return Ok(chunkInfo);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Lấy decision chunks timeout sau {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi lấy decision chunks: {ex.Message}" });
        }
    }

    [HttpGet("decision-chunk")]
    public async Task<IActionResult> GetDecisionChunk([FromQuery] string fileName, [FromQuery] string decisionId, [FromQuery] int chunkIndex, [FromQuery] int chunkSize = 50)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, fileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {fileName} không tồn tại" });
            }

            if (string.IsNullOrWhiteSpace(decisionId))
            {
                return BadRequest(new { error = "Decision ID không được để trống" });
            }

            if (chunkIndex < 0)
            {
                return BadRequest(new { error = "Chunk index phải >= 0" });
            }

            if (chunkSize <= 0 || chunkSize > 200)
            {
                return BadRequest(new { error = "Chunk size phải từ 1 đến 200" });
            }

            var chunk = await _getDecisionChunkUseCase.ExecuteAsync(dmnPath, decisionId, chunkIndex, chunkSize, cts.Token);
            return Ok(chunk);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Lấy decision chunk timeout sau {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi lấy decision chunk: {ex.Message}" });
        }
    }

    [HttpPost("evaluate-chunk")]
    public async Task<IActionResult> EvaluateDecisionChunk([FromBody] EvaluateChunkRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.DmnEvaluationTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.FileName);

            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {request.FileName} không tồn tại" });
            }

            // Lấy chunk
            var chunk = await _getDecisionChunkUseCase.ExecuteAsync(dmnPath, request.DecisionId, request.ChunkIndex, request.ChunkSize, cts.Token);
            
            // Tạo file tạm thời cho chunk
            var tempDir = Path.GetTempPath();
            var tempFileName = $"temp_chunk_{Guid.NewGuid()}.dmn";
            var tempFilePath = Path.Combine(tempDir, tempFileName);
            
            try
            {
                await System.IO.File.WriteAllTextAsync(tempFilePath, chunk.XmlContent, cts.Token);
                
                // Đánh giá chunk (sử dụng decision ID của chunk)
                var chunkDecisionId = $"{request.DecisionId}_chunk_{request.ChunkIndex}";
                var result = await _evaluateDmnUseCase.ExecuteAsync(tempFilePath, chunkDecisionId, request.Inputs);
                
                return Ok(new
                {
                    chunkInfo = chunk,
                    evaluationResult = result,
                    originalDecisionId = request.DecisionId,
                    chunkDecisionId = chunkDecisionId
                });
            }
            finally
            {
                // Xóa file tạm thời
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Đánh giá decision chunk timeout sau {_timeoutSettings.DmnEvaluationTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi khi đánh giá decision chunk: {ex.Message}" });
        }
    }

    [HttpPost("generate-test-streaming")]
    public async Task<IActionResult> GenerateTestStreaming([FromBody] GenerateTestRequest request, [FromQuery] int chunkIndex = 0, [FromQuery] int chunkSize = 50)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.DmnFileName);
            
            if (!System.IO.File.Exists(dmnPath))
            {
                return NotFound(new { error = $"File {request.DmnFileName} không tồn tại" });
            }

            if (chunkSize <= 0 || chunkSize > 200)
            {
                return BadRequest(new { error = "Chunk size phải từ 1 đến 200" });
            }

            // Đếm rules bằng streaming
            var totalRules = await CountRulesWithStreaming(dmnPath, cts.Token);
            
            if (totalRules > 1000)
            {
                return BadRequest(new { 
                    error = $"File DMN quá lớn với {totalRules} rules. Vui lòng sử dụng file nhỏ hơn.",
                    ruleCount = totalRules
                });
            }

            // Tính toán chunk info
            var totalChunks = (int)Math.Ceiling((double)totalRules / chunkSize);
            
            if (chunkIndex >= totalChunks)
            {
                return BadRequest(new { error = $"Chunk index {chunkIndex} vượt quá tổng số chunks ({totalChunks})" });
            }

            // Generate test cases cho chunk này
            var startRule = chunkIndex * chunkSize;
            var endRule = Math.Min(startRule + chunkSize, totalRules);
            
            var testCases = await GenerateTestCasesForChunk(dmnPath, startRule, endRule, cts.Token);

            return Ok(new
            {
                chunkIndex,
                chunkSize,
                totalChunks,
                totalRules,
                startRule,
                endRule,
                testCases,
                hasMore = chunkIndex < totalChunks - 1,
                nextChunkIndex = chunkIndex + 1,
                estimatedTestCases = totalRules * 2, // Ước tính
                streaming = true
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Test generation timed out after {_timeoutSettings.RequestTimeout}ms" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error generating test cases: {ex.Message}" });
        }
    }


} 