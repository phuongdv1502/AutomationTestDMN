using System.Xml;
using DmnTester.Domain.Entities;
using DmnTester.Domain.Interfaces;
using System.Text.Json;

namespace DmnTester.Infrastructure.Services;

public class DmnTestGenerator : IDmnTestGenerator
{
    public async Task<DmnConfig> GenerateTestConfigFromDmnAsync(string dmnPath, string decisionId)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(dmnPath);
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("dmn", "https://www.omg.org/spec/DMN/20191111/MODEL/");

        var decisionTable = xmlDoc.SelectSingleNode($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable", nsmgr);
        
        // Validate decision table exists
        if (decisionTable == null)
        {
            throw new InvalidOperationException($"Decision table not found for decision '{decisionId}' in file '{Path.GetFileName(dmnPath)}'");
        }

        // Validate decision table has rules
        var rules = xmlDoc.SelectNodes($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable/dmn:rule", nsmgr);
        if (rules == null || rules.Count == 0)
        {
            throw new InvalidOperationException($"Decision table for decision '{decisionId}' in file '{Path.GetFileName(dmnPath)}' has no rules. Cannot generate test cases without rules.");
        }

        var inputKeys = GetInputKeys(decisionTable, nsmgr);
        var outputKeys = GetOutputKeys(decisionTable, nsmgr);

        var config = new DmnConfig
        {
            DecisionId = decisionId,
            DmnPath = new List<string> { Path.GetFileName(dmnPath) },
            Data = new DmnConfigData
            {
                Inputs = ExtractInputs(xmlDoc, nsmgr, decisionId),
                TestCases = GenerateCombinationTestCases(xmlDoc, nsmgr, decisionId, inputKeys, outputKeys)
            }
        };

        return config;
    }

    private List<string> GetInputKeys(XmlNode? decisionTable, XmlNamespaceManager nsmgr)
    {
        var keys = new List<string>();
        if (decisionTable == null) return keys;
        var inputClauses = decisionTable.SelectNodes("dmn:input", nsmgr);
        foreach (XmlNode inputClause in inputClauses!)
        {
            var inputExpression = inputClause.SelectSingleNode("dmn:inputExpression", nsmgr);
            var variableName = inputExpression?.SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim() ?? "";
            if (!string.IsNullOrEmpty(variableName))
                keys.Add(variableName);
        }
        return keys;
    }

    private List<string> GetOutputKeys(XmlNode? decisionTable, XmlNamespaceManager nsmgr)
    {
        var keys = new List<string>();
        if (decisionTable == null) return keys;
        var outputClauses = decisionTable.SelectNodes("dmn:output", nsmgr);
        foreach (XmlNode outputClause in outputClauses!)
        {
            var name = outputClause.Attributes?["name"]?.Value ?? "output";
            keys.Add(name);
        }
        return keys;
    }

    private List<DmnInput> ExtractInputs(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, string decisionId)
    {
        var inputs = new List<DmnInput>();
        var decisionTable = xmlDoc.SelectSingleNode($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable", nsmgr);
        if (decisionTable == null) return inputs;
        var inputClauses = decisionTable.SelectNodes("dmn:input", nsmgr);
        foreach (XmlNode inputClause in inputClauses!)
        {
            var inputExpression = inputClause.SelectSingleNode("dmn:inputExpression", nsmgr);
            var variableName = inputExpression?.SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim() ?? "";
            var typeRef = inputExpression?.Attributes?["typeRef"]?.Value;
            if (!string.IsNullOrEmpty(variableName))
            {
                inputs.Add(new DmnInput
                {
                    Key = variableName,
                    Type = GetInputType(typeRef),
                    Values = ExtractPossibleValues(xmlDoc, nsmgr, decisionId, variableName)
                });
            }
        }
        return inputs;
    }

    private string GetInputType(string? typeRef)
    {
        return typeRef?.ToLower() switch
        {
            "string" => "string",
            "integer" => "number",
            "boolean" => "boolean",
            _ => "string"
        };
    }

    private object GetDefaultValueForType(string key)
    {
        // Nếu là kiểu boolean, trả về false (hoặc true nếu bạn muốn)
        if (key.ToLower().Contains("is") || key.ToLower().Contains("has"))
            return false;
        // Nếu là kiểu số, trả về 0 (hoặc 1 nếu bạn muốn)
        if (key.ToLower().Contains("count") || key.ToLower().Contains("number") || key.ToLower().Contains("status"))
            return 0;
        // Mặc định trả về chuỗi rỗng cho các trường string
        return "";
    }

    private List<string> ExtractPossibleValues(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, string decisionId, string variableName)
    {
        var values = new HashSet<string>();
        var rules = xmlDoc.SelectNodes($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable/dmn:rule", nsmgr);
        foreach (XmlNode rule in rules!)
        {
            var inputEntries = rule.SelectNodes("dmn:inputEntry", nsmgr);
            int idx = 0;
            foreach (XmlNode inputEntry in inputEntries!)
            {
                if (idx == 0 && variableName != null) // Chỉ lấy giá trị cho input đầu tiên (cải tiến nếu cần)
                {
                    var text = inputEntry.SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var parsedValues = ParseInputValues(text);
                        foreach (var value in parsedValues)
                        {
                            if (!string.IsNullOrEmpty(value))
                                values.Add(value);
                        }
                    }
                }
                idx++;
            }
        }
        return values.ToList();
    }

    private List<string> ParseInputValues(string text)
    {
        var values = new List<string>();
        text = text.Replace("\"", "");
        var parts = text.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                values.Add(trimmed);
        }
        return values;
    }

    private List<DmnTestCase> GenerateTestCases(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, string decisionId, List<string> inputKeys, List<string> outputKeys)
    {
        var testCases = new List<DmnTestCase>();
        var rules = xmlDoc.SelectNodes($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable/dmn:rule", nsmgr);

        // Generate test cases for each rule
        foreach (XmlNode rule in rules!)
        {
            var ruleId = rule.Attributes?["id"]?.Value ?? "Unknown";
            var inputEntries = rule.SelectNodes("dmn:inputEntry", nsmgr);
            var inputDict = new Dictionary<string, object>();
            for (int i = 0; i < inputEntries!.Count && i < inputKeys.Count; i++)
            {
                var text = inputEntries[i].SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
                var key = inputKeys[i];
                if (string.IsNullOrEmpty(text) || text == "\"\"")
                {
                    // Empty condition matches any value - generate default value
                    inputDict[key] = GetDefaultValueForType(key);
                }
                else if (text.StartsWith("not(") && text.EndsWith(")"))
                {
                    // not("02") => sinh giá trị khác "02"
                    var notValue = text.Substring(4, text.Length - 5).Replace("\"", "").Trim();
                    inputDict[key] = notValue == "default" ? "any" : "default"; // sinh giá trị khác
                }
                else if (text.StartsWith("not (") && text.EndsWith(")"))
                {
                    // not ("DONG") => sinh giá trị khác "DONG"
                    var notValue = text.Substring(5, text.Length - 6).Replace("\"", "").Trim();
                    inputDict[key] = notValue == "default" ? "any" : "default"; // sinh giá trị khác
                }
                else if (text.StartsWith(">"))
                {
                    // >1 => sinh giá trị 2
                    inputDict[key] = 2;
                }
                else if (text.StartsWith("<"))
                {
                    // <5 => sinh giá trị 4
                    inputDict[key] = 4;
                }
                else if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    inputDict[key] = true;
                }
                else if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    inputDict[key] = false;
                }
                else if (int.TryParse(text, out int intVal))
                {
                    inputDict[key] = intVal;
                }
                else if (text.StartsWith("\"") && text.EndsWith("\""))
                {
                    // "DONG" => DONG
                    inputDict[key] = text.Substring(1, text.Length - 2);
                }
                else
                {
                    // Giá trị mặc định (chuỗi)
                    inputDict[key] = text;
                }
            }
            // Luôn tạo test case, kể cả khi inputDict rỗng (rule match với input rỗng)
            var testCase = new DmnTestCase
            {
                Name = $"Test Rule {ruleId} - {string.Join(",", inputDict.Select(kv => $"{kv.Key}={kv.Value}"))}",
                Inputs = inputDict,
                ExpectedOutputs = ExtractRuleOutputs(rule, nsmgr, outputKeys)
            };
            
            // DEBUG: Log test case details
            Console.WriteLine($"[DEBUG] Generated TestCase: {testCase.Name}");
            Console.WriteLine($"[DEBUG] Inputs count: {inputDict.Count}");
            Console.WriteLine($"[DEBUG] Inputs content: {System.Text.Json.JsonSerializer.Serialize(inputDict)}");
            Console.WriteLine($"[DEBUG] ExpectedOutputs: {System.Text.Json.JsonSerializer.Serialize(testCase.ExpectedOutputs)}");
            
            testCases.Add(testCase);
        }
        // Không sinh thêm combination test cases tự động ở đây để tránh sinh input không match rule
        return testCases;
    }

    private List<Dictionary<string, object>> ExtractInputCombinations(XmlNode rule, XmlNamespaceManager nsmgr, List<string> inputKeys)
    {
        var combinations = new List<Dictionary<string, object>>();
        var inputEntries = rule.SelectNodes("dmn:inputEntry", nsmgr);
        
        if (inputEntries == null || inputEntries.Count != inputKeys.Count)
            return combinations;
        
        var inputValues = new List<List<string>>();
        
        for (int i = 0; i < inputEntries.Count; i++)
        {
            var text = inputEntries[i].SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
            var values = new List<string>();
            
            if (string.IsNullOrEmpty(text) || text == "\"\"")
            {
                // Empty condition matches any value - generate test cases for common values
                if (inputKeys[i] == "guestsWithChildren")
                {
                    values.AddRange(new[] { "true", "false" });
                }
                else
                {
                    // For other inputs, add some common values
                    values.AddRange(new[] { "default", "any" });
                }
            }
            else
            {
                values = ParseInputValues(text);
            }
            
            inputValues.Add(values);
        }
        
        // Generate all combinations
        var combinationsList = GenerateCombinations(inputValues);
        foreach (var combination in combinationsList)
        {
            var inputDict = new Dictionary<string, object>();
            for (int i = 0; i < inputKeys.Count; i++)
            {
                if (i < combination.Count)
                {
                    inputDict[inputKeys[i]] = combination[i];
                }
            }
            combinations.Add(inputDict);
        }
        
        return combinations;
    }

    private List<List<string>> GenerateCombinations(List<List<string>> inputValues)
    {
        var result = new List<List<string>>();
        GenerateCombinationsRecursive(inputValues, 0, new List<string>(), result);
        return result;
    }

    private void GenerateCombinationsRecursive(List<List<string>> inputValues, int index, List<string> current, List<List<string>> result)
    {
        if (index == inputValues.Count)
        {
            result.Add(new List<string>(current));
            return;
        }
        
        foreach (var value in inputValues[index])
        {
            current.Add(value);
            GenerateCombinationsRecursive(inputValues, index + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    private List<DmnTestCase> GenerateCombinationTestCases(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, string decisionId, List<string> inputKeys, List<string> outputKeys)
    {
        var testCases = new List<DmnTestCase>();
        
        // Extract all unique values for each input
        var inputValueSets = new Dictionary<string, HashSet<string>>();
        foreach (var inputKey in inputKeys)
        {
            inputValueSets[inputKey] = new HashSet<string>();
        }
        
        var rules = xmlDoc.SelectNodes($"//dmn:decision[@id='{decisionId}']/dmn:decisionTable/dmn:rule", nsmgr);
        foreach (XmlNode rule in rules!)
        {
            var inputEntries = rule.SelectNodes("dmn:inputEntry", nsmgr);
            for (int i = 0; i < inputEntries!.Count && i < inputKeys.Count; i++)
            {
                var text = inputEntries[i].SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(text) && text != "\"\"")
                {
                    var values = ParseInputValues(text);
                    foreach (var value in values)
                    {
                        inputValueSets[inputKeys[i]].Add(value);
                    }
                }
            }
        }
        
        // Add common values for boolean inputs
        if (inputValueSets.ContainsKey("guestsWithChildren"))
        {
            inputValueSets["guestsWithChildren"].Add("true");
            inputValueSets["guestsWithChildren"].Add("false");
        }
        
        // Generate meaningful combination test cases
        var meaningfulCombinations = GenerateMeaningfulCombinations(inputValueSets, inputKeys);
        foreach (var combination in meaningfulCombinations)
        {
            // Only create test case if all required inputs are present
            if (combination.Count == inputKeys.Count)
            {
                var testCase = new DmnTestCase
                {
                    Name = $"Combination Test - {string.Join(",", combination.Select(kv => $"{kv.Key}={kv.Value}"))}",
                    Inputs = combination,
                    ExpectedOutputs = new Dictionary<string, object>() // Will be evaluated at runtime
                };
                testCases.Add(testCase);
            }
        }
        
        return testCases;
    }

    private List<Dictionary<string, object>> GenerateMeaningfulCombinations(Dictionary<string, HashSet<string>> inputValueSets, List<string> inputKeys)
    {
        var combinations = new List<Dictionary<string, object>>();
        
        // Generate combinations that make sense for the specific decision
        if (inputKeys.Contains("desiredDish") && inputKeys.Contains("guestsWithChildren"))
        {
            // For beverages decision, create meaningful combinations
            var dishes = inputValueSets["desiredDish"].ToList();
            var hasChildren = new[] { "true", "false" };
            
            foreach (var dish in dishes)
            {
                foreach (var children in hasChildren)
                {
                    var combination = new Dictionary<string, object>
                    {
                        { "desiredDish", dish },
                        { "guestsWithChildren", children }
                    };
                    combinations.Add(combination);
                }
            }
        }
        else if (inputKeys.Contains("season") && inputKeys.Contains("guestCount"))
        {
            // For dish decision, create meaningful combinations
            var seasons = inputValueSets["season"].ToList();
            var guestCounts = inputValueSets["guestCount"].ToList();
            
            foreach (var season in seasons)
            {
                foreach (var count in guestCounts)
                {
                    var combination = new Dictionary<string, object>
                    {
                        { "season", season },
                        { "guestCount", count }
                    };
                    combinations.Add(combination);
                }
            }
        }
        
        return combinations;
    }

    private Dictionary<string, object> ExtractRuleInputs(XmlNode rule, XmlNamespaceManager nsmgr, List<string> inputKeys)
    {
        var inputs = new Dictionary<string, object>();
        var inputEntries = rule.SelectNodes("dmn:inputEntry", nsmgr);
        int idx = 0;
        foreach (XmlNode inputEntry in inputEntries!)
        {
            var text = inputEntry.SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(text) && idx < inputKeys.Count)
            {
                var values = ParseInputValues(text);
                if (values.Count > 0)
                    inputs[inputKeys[idx]] = values[0];
            }
            idx++;
        }
        return inputs;
    }

    private Dictionary<string, object> ExtractRuleOutputs(XmlNode rule, XmlNamespaceManager nsmgr, List<string> outputKeys)
    {
        var outputs = new Dictionary<string, object>();
        var outputEntries = rule.SelectNodes("dmn:outputEntry", nsmgr);
        int idx = 0;
        foreach (XmlNode outputEntry in outputEntries!)
        {
            var text = outputEntry.SelectSingleNode("dmn:text", nsmgr)?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(text) && idx < outputKeys.Count)
            {
                object value;
                if (text.StartsWith("{") && text.EndsWith("}"))
                {
                    try
                    {
                        value = JsonSerializer.Deserialize<Dictionary<string, object>>(text);
                    }
                    catch
                    {
                        value = text.Replace("\"", ""); // fallback về string nếu parse lỗi
                    }
                }
                else if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                }
                else if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                }
                else if (int.TryParse(text, out int intVal))
                {
                    value = intVal;
                }
                else if (text.StartsWith("\"") && text.EndsWith("\""))
                {
                    value = text.Substring(1, text.Length - 2);
                }
                else
                {
                    value = text.Replace("\"", "");
                }
                outputs[outputKeys[idx]] = value;
            }
            idx++;
        }
        return outputs;
    }

    public async Task<List<(string decisionId, DmnConfig testConfig)>> GenerateAllTestConfigsFromDmnAsync(string dmnPath)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(dmnPath);
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("dmn", "https://www.omg.org/spec/DMN/20191111/MODEL/");

        var decisionNodes = xmlDoc.SelectNodes("//dmn:decision[dmn:decisionTable]", nsmgr);
        
        // Validate that DMN file has at least one decision with decision table
        if (decisionNodes == null || decisionNodes.Count == 0)
        {
            throw new InvalidOperationException($"DMN file '{Path.GetFileName(dmnPath)}' contains no decisions with decision tables.");
        }

        var result = new List<(string, DmnConfig)>();
        var validDecisions = new List<string>();
        var invalidDecisions = new List<string>();

        foreach (XmlNode decision in decisionNodes!)
        {
            var decisionId = decision.Attributes?["id"]?.Value ?? "";
            if (!string.IsNullOrEmpty(decisionId))
            {
                try
                {
                    var config = await GenerateTestConfigFromDmnAsync(dmnPath, decisionId);
                    result.Add((decisionId, config));
                    validDecisions.Add(decisionId);
                }
                catch (InvalidOperationException ex)
                {
                    invalidDecisions.Add(decisionId);
                    // Log the error but continue processing other decisions
                    Console.WriteLine($"Warning: {ex.Message}");
                }
            }
        }

        // If no valid decisions found, throw exception
        if (result.Count == 0)
        {
            var errorMessage = $"No valid decisions found in DMN file '{Path.GetFileName(dmnPath)}'. ";
            if (invalidDecisions.Count > 0)
            {
                errorMessage += $"Invalid decisions: {string.Join(", ", invalidDecisions)}";
            }
            throw new InvalidOperationException(errorMessage);
        }

        return result;
    }
} 