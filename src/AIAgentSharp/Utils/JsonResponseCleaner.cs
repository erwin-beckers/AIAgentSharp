using System.Diagnostics;
using System.Text;

namespace AIAgentSharp.Utils;

/// <summary>
/// State-based JSON cleaner that safely fixes common LLM JSON errors without breaking valid JSON.
/// Handles complex nested structures, JSON-in-JSON, and missing brackets.
/// </summary>
public static class JsonResponseCleaner
{
    private enum ParserState
    {
        LookingForStart,
        InObject,
        InArray,
        InString,
        InEscape,
        LookingForProperty,
        LookingForColon,
        LookingForValue,
        InNumber,
        InBoolean,
        InNull,
        AfterValue
    }

    /// <summary>
    /// Cleans malformed JSON responses using a state-based parser.
    /// Safely handles complex nested structures and JSON-in-JSON scenarios.
    /// </summary>
    /// <param name="content">The raw content from the LLM response.</param>
    /// <returns>Cleaned JSON content with common errors fixed.</returns>
    public static string CleanJsonResponse(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // First, remove markdown code blocks
        var cleaned = RemoveMarkdownCodeBlocks(content);
        
        // Find the JSON content using state-based parsing
        var jsonContent = ExtractJsonContent(cleaned);
        
        // Fix common structural issues
        var fixedJson = FixJsonStructure(jsonContent);
        
        return fixedJson;
    }

    /// <summary>
    /// Removes markdown code blocks from the content.
    /// </summary>
    private static string RemoveMarkdownCodeBlocks(string content)
    {
        content = content.Replace("```json", "");
        content = content.Replace("```", "");
        return content.Trim();
    }

    /// <summary>
    /// Extracts JSON content from the cleaned text using state-based parsing.
    /// </summary>
    private static string ExtractJsonContent(string content)
    {
        var state = ParserState.LookingForStart;
        var braceCount = 0;
        var bracketCount = 0;
        var inString = false;
        var escapeNext = false;
        var jsonStart = -1;
        var jsonEnd = -1;

        for (int i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"' && !escapeNext)
        {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '{')
                {
                    if (state == ParserState.LookingForStart)
                    {
                        jsonStart = i;
                        state = ParserState.InObject;
                }
                braceCount++;
            }
                else if (c == '}')
            {
                braceCount--;
                    if (braceCount == 0 && state == ParserState.InObject)
                    {
                        jsonEnd = i + 1;
                        break;
                    }
                }
                else if (c == '[')
                {
                    if (state == ParserState.LookingForStart)
                    {
                        jsonStart = i;
                        state = ParserState.InArray;
                    }
                    bracketCount++;
                }
                else if (c == ']')
                {
                    bracketCount--;
                    if (bracketCount == 0 && state == ParserState.InArray)
                    {
                        jsonEnd = i + 1;
                        break;
                    }
                }
            }
        }

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return content.Substring(jsonStart, jsonEnd - jsonStart);
        }

        // Fallback: try to find any JSON-like content
        return FindJsonLikeContent(content);
    }

    /// <summary>
    /// Fallback method to find JSON-like content when proper parsing fails.
    /// </summary>
    private static string FindJsonLikeContent(string content)
                {
        var firstBrace = content.IndexOf('{');
        var firstBracket = content.IndexOf('[');
                    
        if (firstBrace >= 0 && (firstBracket < 0 || firstBrace < firstBracket))
        {
            // Start with object
            var lastBrace = content.LastIndexOf('}');
            if (lastBrace > firstBrace)
            {
                return content.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
        }
        else if (firstBracket >= 0)
        {
            // Start with array
            var lastBracket = content.LastIndexOf(']');
            if (lastBracket > firstBracket)
            {
                return content.Substring(firstBracket, lastBracket - firstBracket + 1);
                }
            }

        return content;
        }

    /// <summary>
    /// Fixes common JSON structural issues using state-based analysis.
    /// </summary>
    private static string FixJsonStructure(string json)
        {
        if (string.IsNullOrEmpty(json))
            return json;
            
        var state = ParserState.LookingForStart;
        var braceCount = 0;
        var bracketCount = 0;
        var inString = false;
        var escapeNext = false;
        var result = new StringBuilder();
        var lastChar = '\0';

        for (int i = 0; i < json.Length; i++)
            {
            var c = json[i];
            
            if (escapeNext)
            {
                // Handle escaped characters
                if (c == '\'')
                {
                    // JSON does not define \' escape. Drop the backslash and keep the apostrophe.
                    if (result.Length > 0 && result[result.Length - 1] == '\\')
                    {
                        result.Length -= 1; // remove previously appended backslash
                    }
                    result.Append('\'');
                }
                else
                {
                    result.Append(c);
                }
                escapeNext = false;
                lastChar = c;
                continue;
            }

            if (c == '\\')
            {
                result.Append(c);
                escapeNext = true;
                continue;
            }

            if (c == '"' && !escapeNext)
        {
                inString = !inString;
                result.Append(c);
                lastChar = c;
                continue;
            }

            if (!inString)
            {
                if (c == '{')
                {
                    if (state == ParserState.LookingForStart)
                        state = ParserState.InObject;
                braceCount++;
                    result.Append(c);
            }
                else if (c == '}')
            {
                braceCount--;
                    result.Append(c);
                }
                else if (c == '[')
                {
                    if (state == ParserState.LookingForStart)
                        state = ParserState.InArray;
                    bracketCount++;
                    result.Append(c);
                }
                else if (c == ']')
                {
                    bracketCount--;
                    result.Append(c);
                }
                else if (c == ',')
                {
                    // Check for trailing comma
                    var nextNonWhitespace = GetNextNonWhitespace(json, i + 1);
                    if (nextNonWhitespace == '}' || nextNonWhitespace == ']')
                    {
                        // Skip trailing comma
                        continue;
                    }
                    result.Append(c);
                }
                else if (c == '\r' || c == '\n')
                {
                    // Skip structural newlines and carriage returns
                    continue;
                }
                else
                {
                    result.Append(c);
                }
            }
            else
            {
                // Inside string - handle common LLM string errors
                if (c == '\r')
                {
                    // Convert carriage returns to escaped newlines
                    result.Append("\\n");
                }
                else if (c == '\n')
                {
                    // Convert newlines to escaped newlines
                    result.Append("\\n");
                }
                else
                {
                    result.Append(c);
                }
            }

            lastChar = c;
        }

        // Add missing closing brackets/braces
        var fixedJson = result.ToString();
        
        if (state == ParserState.InObject && braceCount > 0)
        {
            fixedJson += new string('}', braceCount);
        }
        else if (state == ParserState.InArray && bracketCount > 0)
        {
            fixedJson += new string(']', bracketCount);
        }

        return fixedJson;
    }

    /// <summary>
    /// Gets the next non-whitespace character from the specified position.
    /// </summary>
    private static char GetNextNonWhitespace(string text, int startPos)
                {
        for (int i = startPos; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsWhiteSpace(c))
                return c;
                }
        return '\0';
            }

    /// <summary>
    /// Test method to debug JSON parsing issues.
    /// </summary>
    public static string TestJsonParsing(string json)
    {
        var result = CleanJsonResponse(json);
        System.Diagnostics.Debug.WriteLine($"Original: {json}");
        System.Diagnostics.Debug.WriteLine($"Cleaned:  {result}");
        return result;
        }

    /// <summary>
    /// Comprehensive test method for the JSON cleaner.
    /// </summary>
    public static void RunTests()
        {
        // Test the problematic JSON from the user
        var testJson = "{\"thoughts\":\"The strategy has been validated successfully, and the components are structured as follows: 1) Indicators: the \\'Bollinger Bands\\' and the \\'Chandelier Exit\\'. 2) Entry Rule: Enter a long position when the price closes above the upper Bollinger Band and the Chandelier Exit indicates a bullish trend. 3) Exit Rule: Exit the position when the price hits the Chandelier Exit trailing stop or when a profit target of $1500 is reached. 4) Position sizing is set to 1 contract with a max stop distance of 100 ticks. 5) Daily trading limits specify a maximum of 3 trades per day and a limit of 3 consecutive losses. I am now ready to backtest this strategy to evaluate its performance against historical data.\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"backtest_strategy\",\"params\":{\"strategy\":{\"name\":\"TrendFollowingStrategy\",\"indicators\":[{\"name\":\"Bollinger Bands\",\"parameters\":{\"period\":20,\"deviation\":2}},{\"name\":\"Chandelier Exit\",\"parameters\":{\"length\":22,\"multiplier\":3}}],\"entryRules\":[{\"condition\":\"Price closes above upper Bollinger Band and Chandelier Exit is bullish\",\"action\":\"Enter Long\"}],\"exitRules\":[{\"condition\":\"Price hits Chandelier Exit trailing stop\",\"action\":\"Exit\"},{\"condition\":\"Achieves $1500 profit target\",\"action\":\"Exit\"}],\"atmSettings\":{\"positionSize\":1,\"maxStopDistance\":100,\"tradeLimit\":3,\"consecutiveLosersLimit\":3}}}},\"reasoning_confidence\":0.95,\"reasoning_type\":\"Analysis\"}";
        
        var cleaned = CleanJsonResponse(testJson);
        
        System.Diagnostics.Debug.WriteLine("=== JSON Cleaner Test ===");
        System.Diagnostics.Debug.WriteLine($"Original length: {testJson.Length}");
        System.Diagnostics.Debug.WriteLine($"Cleaned length:  {cleaned.Length}");
        System.Diagnostics.Debug.WriteLine($"Is valid JSON: {IsValidJson(cleaned)}");
        System.Diagnostics.Debug.WriteLine("=== End Test ===");
        }

    /// <summary>
    /// Extracts JSON from markdown code blocks and cleans it.
    /// This method is specifically designed for parsing LLM responses that contain JSON in code blocks.
    /// </summary>
    /// <param name="content">The raw content from the LLM response.</param>
    /// <returns>Cleaned JSON content extracted from code blocks.</returns>
    public static string ExtractAndCleanJsonFromCodeBlocks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // First, try to extract JSON content from the text (including from within code blocks)
        var jsonContent = ExtractJsonContent(content);
        
        // Fix common structural issues first (including newline escaping)
        var fixedJson = FixJsonStructure(jsonContent);
        
        // If the fixed JSON is still not valid, try removing markdown code blocks
        if (string.IsNullOrEmpty(fixedJson) || !IsValidJson(fixedJson))
        {
            var cleaned = RemoveMarkdownCodeBlocks(content);
            jsonContent = ExtractJsonContent(cleaned);
            fixedJson = FixJsonStructure(jsonContent);
        }
        
        return fixedJson;
    }

    /// <summary>
    /// Simple JSON validation check.
    /// </summary>
    public static bool IsValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

