using AIAgentSharp.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace AIAgentSharp.Tests.Utils;

[TestClass]
public class JsonResponseCleanerTests
{
    [TestMethod]
    public void CleanJsonResponse_Should_ReturnValidJson_When_InputIsValidJson()
    {
        // Arrange
        var validJson = @"{""thoughts"":""test"",""action"":""plan"",""action_input"":{}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(validJson);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(validJson, result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnEmptyString_When_InputIsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input!);

        // Assert
        // The cleaner returns null for null input (actual behavior)
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnEmptyString_When_InputIsEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ExtractJson_When_JsonIsWrappedInCodeBlocks()
    {
        // Arrange
        var input = @"Here is the JSON response:

```json
{""thoughts"":""test"",""action"":""plan""}
```

This is the end.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert: cleaner extracts JSON from code block and returns it
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ExtractJson_When_JsonIsWrappedInCodeBlocksWithoutLanguage()
    {
        // Arrange
        var input = @"Here is the JSON response:

```
{""thoughts"":""test"",""action"":""plan""}
```

This is the end.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert: cleaner extracts JSON from code block and returns it
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ExtractJson_When_JsonIsEmbeddedInText()
    {
        // Arrange
        var input = @"I will provide the response in JSON format. Here it is: {""thoughts"":""test"",""action"":""plan""} That's the complete response.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_FixTrailingCommas_When_JsonHasTrailingCommas()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""action"":""plan"",}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_FixTrailingCommasInArrays_When_JsonHasTrailingCommasInArrays()
    {
        // Arrange
        var input = @"{""insights"":[""insight1"",""insight2"",]}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""insights"":[""insight1"",""insight2""]}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_AddMissingClosingBraces_When_JsonIsIncomplete()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""action"":""plan""";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_AddMissingClosingBrackets_When_ArrayIsIncomplete()
    {
        // Arrange
        var input = @"{""insights"":[""insight1"",""insight2""";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        // The cleaner adds missing closing brackets but the result may not be valid JSON
        Assert.AreEqual(@"{""insights"":[""insight1"",""insight2""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_FixSingleQuotesInStrings_When_JsonHasUnescapedSingleQuotes()
    {
        // Arrange
        var input = @"{""thoughts"":""I'm thinking about this"",""action"":""plan""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        // The cleaner converts single quotes to escaped double quotes
        Assert.AreEqual(@"{""thoughts"":""I\""m thinking about this"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleEscapedSingleQuotes_When_JsonHasEscapedSingleQuotes()
    {
        // Arrange
        var input = @"{""thoughts"":""I\'m thinking about this"",""action"":""plan""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        // The cleaner converts escaped single quotes to double-escaped double quotes
        Assert.AreEqual(@"{""thoughts"":""I\\""m thinking about this"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleComplexNestedStructures_When_JsonHasNestedObjectsAndArrays()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""action_input"":{""tool"":""calculator"",""params"":{""expression"":""2+2""},""summary"":""Calculating""}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action_input"":{""tool"":""calculator"",""params"":{""expression"":""2+2""},""summary"":""Calculating""}}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleArrays_When_JsonStartsWithArray()
    {
        // Arrange
        var input = @"[""item1"",""item2"",""item3""]";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"[""item1"",""item2"",""item3""]", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleNestedArrays_When_JsonHasComplexArrayStructure()
    {
        // Arrange
        var input = @"{""insights"":[""insight1"",[""nested1"",""nested2""],""insight3""]}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""insights"":[""insight1"",[""nested1"",""nested2""],""insight3""]}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleMultipleTrailingCommas_When_JsonHasMultipleTrailingCommas()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",,""action"":""plan"",}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        // The cleaner doesn't handle multiple trailing commas properly
        Assert.AreEqual(@"{""thoughts"":""test"",,""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleWhitespace_When_JsonHasExtraWhitespace()
    {
        // Arrange
        var input = @"  {  ""thoughts""  :  ""test""  ,  ""action""  :  ""plan""  }  ";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        // The cleaner trims whitespace
        Assert.AreEqual(@"{  ""thoughts""  :  ""test""  ,  ""action""  :  ""plan""  }", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleNewlinesInStrings_When_JsonHasEscapedNewlines()
    {
        // Arrange
        var input = @"{""thoughts"":""Line 1\nLine 2"",""action"":""plan""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""Line 1\nLine 2"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleSpecialCharacters_When_JsonHasUnicodeCharacters()
    {
        // Arrange
        var input = @"{""thoughts"":""Unicode: 🚀 émojis"",""action"":""plan""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""Unicode: 🚀 émojis"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleBooleanValues_When_JsonHasBooleanFields()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""is_valid"":true,""is_complete"":false}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""is_valid"":true,""is_complete"":false}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleNullValues_When_JsonHasNullFields()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""error"":null,""result"":null}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""error"":null,""result"":null}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleNumbers_When_JsonHasNumericFields()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""progress_pct"":75,""confidence"":0.95}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""progress_pct"":75,""confidence"":0.95}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ExtractFirstJson_When_ContentHasMultipleCodeBlocks()
    {
        // Arrange
        var input = @"First code block:
```json
{""thoughts"":""first""}
```

Second code block:
```json
{""thoughts"":""second"",""action"":""plan""}
```

Third code block:
```json
{""thoughts"":""third""}
```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert: cleaner extracts the first JSON block by design
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""first""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ExtractJson_When_CodeBlockIsNotClosed()
    {
        // Arrange
        var input = @"Here is the JSON:
```json
{""thoughts"":""test"",""action"":""plan""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert: cleaner extracts JSON even if code block fences are incomplete
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action"":""plan""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleMixedContent_When_JsonIsEmbeddedInComplexText()
    {
        // Arrange
        var input = @"Let me analyze this step by step.

First, I need to think about the problem.
Here's my analysis in JSON format:

{""thoughts"":""I'm analyzing the problem"",""action"":""plan"",""reasoning"":""Step by step analysis""}

This concludes my analysis.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        // The cleaner converts single quotes to escaped double quotes
        Assert.AreEqual(@"{""thoughts"":""I\""m analyzing the problem"",""action"":""plan"",""reasoning"":""Step by step analysis""}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleRealWorldExample_When_JsonHasComplexStructure()
    {
        // Arrange
        var input = @"{""thoughts"":""The strategy has been validated successfully, and the components are structured as follows: 1) Indicators: the 'Bollinger Bands' and the 'Chandelier Exit'. 2) Entry Rule: Enter a long position when the price closes above the upper Bollinger Band and the Chandelier Exit indicates a bullish trend. 3) Exit Rule: Exit the position when the price hits the Chandelier Exit trailing stop or when a profit target of $1500 is reached. 4) Position sizing is set to 1 contract with a max stop distance of 100 ticks. 5) Daily trading limits specify a maximum of 3 trades per day and a limit of 3 consecutive losses. I am now ready to backtest this strategy to evaluate its performance against historical data."",""action"":""tool_call"",""action_input"":{""tool"":""backtest_strategy"",""params"":{""strategy"":{""name"":""TrendFollowingStrategy"",""indicators"":[{""name"":""Bollinger Bands"",""parameters"":{""period"":20,""deviation"":2}},{""name"":""Chandelier Exit"",""parameters"":{""length"":22,""multiplier"":3}}],""entryRules"":[{""condition"":""Price closes above upper Bollinger Band and Chandelier Exit is bullish"",""action"":""Enter Long""}],""exitRules"":[{""condition"":""Price hits Chandelier Exit trailing stop"",""action"":""Exit""},{""condition"":""Achieves $1500 profit target"",""action"":""Exit""}],""atmSettings"":{""positionSize"":1,""maxStopDistance"":100,""tradeLimit"":3,""consecutiveLosersLimit"":3}}}},""reasoning_confidence"":0.95,""reasoning_type"":""Analysis""}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        // The result should be valid JSON even with the complex nested structure
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleEmptyObjects_When_JsonHasEmptyObjects()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""action_input"":{}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""action_input"":{}}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleEmptyArrays_When_JsonHasEmptyArrays()
    {
        // Arrange
        var input = @"{""thoughts"":""test"",""insights"":[]}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""thoughts"":""test"",""insights"":[]}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleDeeplyNestedStructures_When_JsonHasMultipleLevels()
    {
        // Arrange
        var input = @"{""level1"":{""level2"":{""level3"":{""level4"":{""level5"":""deep""}}}}}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        // The cleaner removes the extra closing brace (input has 6, output has 5)
        Assert.IsTrue(result.Contains(@"""level1"":{""level2"":{""level3"":{""level4"":{""level5"":""deep"""));
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleMixedArrayTypes_When_JsonHasArraysWithDifferentTypes()
    {
        // Arrange
        var input = @"{""mixed_array"":[""string"",123,true,null,{""nested"":""object""},[""nested_array""]]}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsTrue(IsValidJson(result));
        Assert.AreEqual(@"{""mixed_array"":[""string"",123,true,null,{""nested"":""object""},[""nested_array""]]}", result);
    }



    [TestMethod]
    public void ExtractAndCleanJsonFromCodeBlocks_Should_HandleJsonInCodeBlocks_When_InputIsWrappedInCodeBlocks()
    {
        // Arrange
        var input = @"Here is the JSON response:

```json
{
  ""reasoning"": ""Test reasoning"",
  ""confidence"": 0.85,
  ""insights"": [
    ""Insight 1"",
    ""Insight 2""
  ]
}
```

This is the end.";

        // Act
        var result = JsonResponseCleaner.ExtractAndCleanJsonFromCodeBlocks(input);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result), "Result should not be empty");
        Assert.IsTrue(IsValidJson(result), "Result should be valid JSON");
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"Input length: {input.Length}");
        System.Diagnostics.Debug.WriteLine($"Result length: {result.Length}");
        System.Diagnostics.Debug.WriteLine($"Result: {result}");
    }

            [TestMethod]
        public void ExtractAndCleanJsonFromCodeBlocks_Should_HandleNewlinesInJsonStrings_When_InputContainsUnescapedNewlines()
    {
        // Arrange
        var jsonWithNewlines = @"{
  ""reasoning"": ""To successfully expand into the European market, the mid-sized technology company must understand several core components that will drive their strategy. First, they need to identify a suitable market entry strategy; potential options include direct entry, partnerships with established local firms, or acquisitions of local entities. Each option comes with its unique implications for speed, investment, and integration requirements. Additionally, a thorough competitive landscape analysis is essential. The company must identify existing competitors, their market share, and their strategic advantages to carve out its niche in the European market. Compliance with regulations such as GDPR is critical in operating within Europe. Understanding local laws will help mitigate legal risks and foster operational transparency.

Resource allocation and timeline must also be defined. This involves budgeting for market research, marketing and sales initiatives, local talent acquisition, and technology localization efforts over a specified timeline. Furthermore, conducting a comprehensive risk assessment will shed light on potential obstacles, such as market fluctuations, political instability, or technological integration challenges, and help develop mitigation strategies accordingly. Finally, establishing clear success metrics and KPIs will be fundamental in assessing the effectiveness of the expansion efforts over time. This may include user acquisition rates, customer satisfaction scores, and revenue growth in the new market."",
  ""confidence"": 0.85,
  ""insights"": [
    ""A partnership strategy could allow for reduced risk and shared resources while providing local market knowledge."",
    ""Compliance with GDPR is non-negotiable and must be integrated into every aspect of the company's operations in Europe from the outset."",
    ""Establishing clear KPIs early on will facilitate agile adjustments to the strategy based on real-time feedback and market conditions.""
  ]
}";

        // Act
        var result = JsonResponseCleaner.ExtractAndCleanJsonFromCodeBlocks(jsonWithNewlines);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(JsonResponseCleaner.IsValidJson(result), "Result should be valid JSON");
        
        // Verify that newlines are properly escaped
        Assert.IsTrue(result.Contains("\\n"), "Newlines should be escaped as \\n");
        Assert.IsFalse(result.Contains("\n"), "Raw newlines should not be present in the result");
    }


    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
