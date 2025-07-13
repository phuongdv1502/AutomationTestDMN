using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;
using System.Text.Json;
using System.Xml.Linq;
using System.Net;

namespace DmnTester.Infrastructure.Services;

public class DmnEngine : IDmnEngine
{
    private readonly int _evaluationTimeout;
    private readonly int _fileReadTimeout;

    public DmnEngine(int evaluationTimeout = 10000, int fileReadTimeout = 5000)
    {
        _evaluationTimeout = evaluationTimeout;
        _fileReadTimeout = fileReadTimeout;
    }

    public async Task<List<DmnConfig>> LoadConfigsAsync(string configPath)
    {
        var configs = new List<DmnConfig>();
        
        if (!Directory.Exists(configPath))
        {
            throw new DirectoryNotFoundException($"Config path not found: {configPath}");
        }

        var configFiles = Directory.GetFiles(configPath, "*.json");
        
        foreach (var configFile in configFiles)
        {
            try
            {
                var config = await LoadConfigAsync(configFile);
                configs.Add(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config {configFile}: {ex.Message}");
            }
        }

        return configs;
    }

    public async Task<DmnConfig> LoadConfigAsync(string configFile)
    {
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"Config file not found: {configFile}");
        }

        using var cts = new CancellationTokenSource(_fileReadTimeout);
        var jsonContent = await File.ReadAllTextAsync(configFile, cts.Token);
        
        var configDto = JsonSerializer.Deserialize<DmnConfigDto>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (configDto == null)
        {
            throw new InvalidOperationException($"Failed to deserialize config file: {configFile}");
        }

        // Convert DTO to Domain Entity
        var config = new DmnConfig
        {
            DecisionId = configDto.DecisionId,
            DmnPath = configDto.DmnPath,
            Data = new DmnConfigData
            {
                Inputs = configDto.Data.Inputs.Select(i => new DmnInput
                {
                    Key = i.Key,
                    Type = i.Type,
                    Values = i.Values
                }).ToList(),
                TestCases = configDto.Data.TestCases.Select(t => new DmnTestCase
                {
                    Name = t.Name,
                    Inputs = t.Inputs,
                    ExpectedOutputs = t.ExpectedOutputs.Count > 0 ? t.ExpectedOutputs : 
                        (t.Results.Count > 0 ? t.Results[0].Outputs : new Dictionary<string, object>())
                }).ToList()
            }
        };

        return config;
    }

    public async Task<DmnEvalResult> EvaluateAsync(string dmnPath, string decisionId, Dictionary<string, object> inputs)
    {
        using var cts = new CancellationTokenSource(_evaluationTimeout);
        
        try
        {
            if (!File.Exists(dmnPath))
            {
                return new DmnEvalResult
                {
                    DecisionId = decisionId,
                    IsSuccess = false,
                    ErrorMessage = $"DMN file not found: {dmnPath}"
                };
            }

            var xmlContent = await File.ReadAllTextAsync(dmnPath, cts.Token);
            var doc = XDocument.Parse(xmlContent);
            
            // Find the decision table for the specified decision
            var decision = doc.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "decision" && x.Attribute("id")?.Value == decisionId);
            
            if (decision == null)
            {
                return new DmnEvalResult
                {
                    DecisionId = decisionId,
                    IsSuccess = false,
                    ErrorMessage = $"Decision '{decisionId}' not found in DMN file"
                };
            }

            var decisionTable = decision.Descendants().FirstOrDefault(x => x.Name.LocalName == "decisionTable");
            if (decisionTable == null)
            {
                return new DmnEvalResult
                {
                    DecisionId = decisionId,
                    IsSuccess = false,
                    ErrorMessage = $"Decision table not found for decision '{decisionId}'"
                };
            }

            // Get output name
            var output = decisionTable.Descendants().FirstOrDefault(x => x.Name.LocalName == "output");
            var outputName = output?.Attribute("name")?.Value ?? "output";
            
            Console.WriteLine($"Evaluating decision '{decisionId}' with inputs: {string.Join(", ", inputs.Select(kv => $"{kv.Key}={kv.Value}"))}");

            // Evaluate rules
            var outputs = new Dictionary<string, object>();
            var rules = decisionTable.Descendants().Where(x => x.Name.LocalName == "rule").ToList();
            var matchingOutputs = new List<object>();
            
            Console.WriteLine($"Found {rules.Count} rules to evaluate");
            
            foreach (var rule in rules)
            {
                //cts.Token.ThrowIfCancellationRequested();
                
                var isMatch = EvaluateRule(rule, inputs);
                Console.WriteLine($"Rule {rule.Attribute("id")?.Value}: match = {isMatch}");
                
                if (isMatch)
                {
                    var outputEntry = rule.Descendants().Where(x => x.Name.LocalName == "outputEntry");
                    if (outputEntry != null)
                    {
                        var lst = outputEntry.Descendants().Where(x => x.Name.LocalName == "text").ToList();
                        foreach (var outputValue in lst)
                        {
                            if (!string.IsNullOrEmpty(outputValue.Value))
                            {
                                // Parse output value - handle JSON objects and simple values
                                object parsedValue;
                                var cleanValue = outputValue.Value.Trim('"');

                                if (cleanValue.StartsWith("{") && cleanValue.EndsWith("}"))
                                {
                                    try
                                    {
                                        parsedValue = JsonSerializer.Deserialize<Dictionary<string, object>>(cleanValue);
                                    }
                                    catch
                                    {
                                        parsedValue = cleanValue; // fallback to string if parse fails
                                    }
                                }
                                else if (cleanValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                                    parsedValue = true;
                                }
                                else if (cleanValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                                {
                                    parsedValue = false;
                                }
                                else if (int.TryParse(cleanValue, out int intVal))
                                {
                                    parsedValue = intVal;
                                }
                                else
                                {
                                    parsedValue = cleanValue;
                                }

                                matchingOutputs.Add(parsedValue);
                                Console.WriteLine($"  Output: {parsedValue}");
                            }
                        
                        }
                    }
                }
            }
            
            // If multiple outputs, return as list, otherwise as single value
            if (matchingOutputs.Count > 1)
            {
                outputs[outputName] = matchingOutputs;
            }
            else if (matchingOutputs.Count == 1)
            {
                outputs[outputName] = matchingOutputs[0];
            }
            
            Console.WriteLine($"Final outputs: {string.Join(", ", outputs.Select(kv => $"{kv.Key}={kv.Value}"))}");

            return new DmnEvalResult
            {
                DecisionId = decisionId,
                Outputs = outputs,
                IsSuccess = true
            };
        }
        catch (OperationCanceledException ex)
        {
            return new DmnEvalResult
            {
                DecisionId = decisionId,
                IsSuccess = false,
                ErrorMessage = $"DMN evaluation timed out after {_evaluationTimeout}ms"
            };
        }
        catch (Exception ex)
        {
            return new DmnEvalResult
            {
                DecisionId = decisionId,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private bool EvaluateRule(XElement rule, Dictionary<string, object> inputs)
    {
        var inputEntries = rule.Descendants().Where(x => x.Name.LocalName == "inputEntry").ToList();
        
        // Find input expressions from the decision table (parent of rule)
        var decisionTable = rule.Parent;
        var inputExpressions = decisionTable?.Descendants().Where(x => x.Name.LocalName == "input").ToList();
        
        Console.WriteLine($"  Rule evaluation: {inputEntries.Count} input entries, {inputExpressions?.Count ?? 0} input expressions");
        
        if (inputExpressions == null || inputEntries.Count != inputExpressions.Count)
            return false;
        
        for (int i = 0; i < inputExpressions.Count; i++)
        {
            var inputExpr = inputExpressions[i];
            var inputEntry = inputEntries[i];
            
            var inputKey = inputExpr.Descendants().FirstOrDefault(x => x.Name.LocalName == "text")?.Value;
            var entryValue = inputEntry.Descendants().FirstOrDefault(x => x.Name.LocalName == "text")?.Value;
            
            if (!string.IsNullOrEmpty(entryValue))
            {
                entryValue = WebUtility.HtmlDecode(entryValue);
            }
            
            Console.WriteLine($"    Input {i}: key='{inputKey}', condition='{entryValue}', value='{inputs.GetValueOrDefault(inputKey)}'");
            
            if (string.IsNullOrEmpty(inputKey) || string.IsNullOrEmpty(entryValue)) 
                continue;
            
            if (!inputs.ContainsKey(inputKey)) 
                return false;
            
            var inputValue = inputs[inputKey];
            var conditionMatch = EvaluateCondition(entryValue, inputValue);
            Console.WriteLine($"    Condition match: {conditionMatch}");
            
            if (!conditionMatch) 
                return false;
        }
        
        return true;
    }
    
    private bool EvaluateCondition(string condition, object value)
    {
        // Handle empty condition (matches anything)
        if (string.IsNullOrEmpty(condition) || condition.Trim() == "\"\"")
            return true;
        
        // Handle not() conditions
        if (condition.StartsWith("not(") && condition.EndsWith(")"))
        {
            var notValue = condition.Substring(4, condition.Length - 5).Replace("\"", "").Trim();
            return value.ToString() != notValue;
        }
        
        // Handle quoted strings
        if (condition.Contains("\""))
        {
            var expectedValues = condition.Split(',')
                .Select(x => x.Trim().Trim('"'))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
            
            if (expectedValues.Count > 0)
            {
                return expectedValues.Any(expected => value.ToString() == expected);
            }
        }
        
        // Handle boolean values - both string and actual boolean
        if (condition.Trim() == "true" || condition.Trim() == "false")
        {
            var expectedBool = condition.Trim() == "true";
            var actualValue = value.ToString().ToLower();
            var actualBool = actualValue == "true" || actualValue == "1";
            return expectedBool == actualBool;
        }
        
        // Handle numeric comparisons
        if (condition.Contains("<="))
        {
            var limit = condition.Replace("<=", "").Trim();
            if (int.TryParse(limit, out int limitValue) && int.TryParse(value.ToString(), out int inputValue))
            {
                return inputValue <= limitValue;
            }
        }
        
        if (condition.Contains(">"))
        {
            var limit = condition.Replace(">", "").Trim();
            if (int.TryParse(limit, out int limitValue) && int.TryParse(value.ToString(), out int inputValue))
            {
                return inputValue > limitValue;
            }
        }
        
        if (condition.Contains("[") && condition.Contains("]"))
        {
            // Handle range like [5..8]
            var range = condition.Trim('[', ']');
            var parts = range.Split("..");
            if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
            {
                if (int.TryParse(value.ToString(), out int inputValue))
                {
                    return inputValue >= min && inputValue <= max;
                }
            }
        }
        
        // Default string comparison
        return value.ToString() == condition.Trim();
    }
} 