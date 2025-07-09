using Microsoft.AspNetCore.Mvc;
using DmnTester.Presentation.Models;
using DmnTester.Application.UseCases;
using DmnTester.Infrastructure.Services;
using DmnTester.Domain.Interfaces;

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
    private readonly IConfiguration _configuration;
    private readonly TimeoutSettings _timeoutSettings;

    public DmnController(
        GetConfigsUseCase getConfigsUseCase,
        GetConfigUseCase getConfigUseCase,
        EvaluateDmnUseCase evaluateDmnUseCase,
        TestDmnUseCase testDmnUseCase,
        GenerateTestFromDmnUseCase generateTestFromDmnUseCase,
        IConfiguration configuration,
        TimeoutSettings timeoutSettings)
    {
        _getConfigsUseCase = getConfigsUseCase;
        _getConfigUseCase = getConfigUseCase;
        _evaluateDmnUseCase = evaluateDmnUseCase;
        _testDmnUseCase = testDmnUseCase;
        _generateTestFromDmnUseCase = generateTestFromDmnUseCase;
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

    [HttpPost("generate-test")]
    public async Task<IActionResult> GenerateTestFromDmn([FromBody] GenerateTestRequest request)
    {
        using var cts = new CancellationTokenSource(_timeoutSettings.RequestTimeout);
        try
        {
            var dmnBasePath = _configuration["DmnFilesPath"] ?? "dmn";
            var absoluteDmnPath = Path.Combine(Directory.GetCurrentDirectory(), dmnBasePath);
            var dmnPath = Path.Combine(absoluteDmnPath, request.DmnFileName);
            var configs = await _generateTestFromDmnUseCase.ExecuteAllAsync(dmnPath);
            var result = configs.Select(x => new { decisionId = x.decisionId, testConfig = x.testConfig}).ToList();
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = $"Test generation timed out after {_timeoutSettings.RequestTimeout}ms" });
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
                foreach (var testCase in item.TestCases)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    
                    if(!testCase.Inputs.Any())
                        Console.WriteLine($"[DEBUG] BatchTest - TestCase: {testCase.Name}");
                    // Debug log để xem inputs thực tế
                    Console.WriteLine($"[DEBUG] BatchTest - TestCase: {testCase.Name}");
                    Console.WriteLine($"[DEBUG] BatchTest - Inputs: {System.Text.Json.JsonSerializer.Serialize(testCase.Inputs)}");
                    Console.WriteLine($"[DEBUG] BatchTest - Inputs Count: {testCase.Inputs.Count}");
                    
                    var evalResult = await _evaluateDmnUseCase.ExecuteAsync(dmnPath, item.DecisionId, testCase.Inputs);
                    var pass = CompareOutputs(evalResult.Outputs, testCase.ExpectedOutputs);
                    
                    // Debug log để xem comparison
                    Console.WriteLine($"[DEBUG] CompareOutputs - TestCase: {testCase.Name}");
                    Console.WriteLine($"[DEBUG] Expected: {System.Text.Json.JsonSerializer.Serialize(testCase.ExpectedOutputs)}");
                    Console.WriteLine($"[DEBUG] Actual: {System.Text.Json.JsonSerializer.Serialize(evalResult.Outputs)}");
                    Console.WriteLine($"[DEBUG] Pass: {pass}");
                    var resultItem = new
                    {
                        name = testCase.Name,
                        pass,
                        actualOutputs = evalResult.Outputs,
                        expectedOutputs = testCase.ExpectedOutputs,
                        diff = GetDiff(evalResult.Outputs, testCase.ExpectedOutputs),
                        inputs = testCase.Inputs
                    };
                    
                    // Debug log để xem response thực tế
                    Console.WriteLine($"[DEBUG] Response - TestCase: {testCase.Name}");
                    Console.WriteLine($"[DEBUG] Response - Inputs: {System.Text.Json.JsonSerializer.Serialize(resultItem.inputs)}");
                    Console.WriteLine($"[DEBUG] Response - Inputs Count: {resultItem.inputs.Count}");
                    
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

    private bool CompareOutputs(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        // If expected outputs is empty, treat as dynamic test - always pass if we get any output
        if (expected.Count == 0)
        {
            return actual.Count > 0; // Pass if we get any output
        }
        
        if (actual.Count != expected.Count) return false;
        foreach (var kv in expected)
        {
            if (!actual.TryGetValue(kv.Key, out var actualValue))
                return false;
                
            // Handle case where expected is a single value but actual might be a list
            if (actualValue is List<string> actualList)
            {
                // If expected is a single value, check if it's in the list
                if (actualList.Contains(kv.Value?.ToString()))
                    continue;
                else
                    return false;
            }
            else
            {
                // Direct comparison for single values
                if (actualValue?.ToString() != kv.Value?.ToString())
                    return false;
            }
        }
        return true;
    }

    private object GetDiff(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        var diff = new Dictionary<string, object>();
        foreach (var kv in expected)
        {
            if (!actual.TryGetValue(kv.Key, out var v))
            {
                diff[kv.Key] = new { expected = kv.Value, actual = v };
                continue;
            }
            
            // Handle case where expected is a single value but actual might be a list
            if (v is List<string> actualList)
            {
                // If expected is a single value, check if it's in the list
                if (!actualList.Contains(kv.Value?.ToString()))
                {
                    diff[kv.Key] = new { expected = kv.Value, actual = v };
                }
            }
            else
            {
                // Direct comparison for single values
                if (v?.ToString() != kv.Value?.ToString())
                {
                    diff[kv.Key] = new { expected = kv.Value, actual = v };
                }
            }
        }
        return diff;
    }
} 