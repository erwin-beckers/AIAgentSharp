namespace AIAgentSharp.Utils;

/// <summary>
/// Utility class for cleaning malformed JSON responses from LLMs.
/// </summary>
public static class JsonResponseCleaner
{
    /// <summary>
    /// Cleans malformed JSON responses by extracting the first valid JSON object.
    /// Removes markdown code blocks and handles duplicate JSON objects.
    /// </summary>
    /// <param name="content">The raw content from the LLM response.</param>
    /// <returns>Cleaned JSON content containing only the first valid JSON object.</returns>
    public static string CleanJsonResponse(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Remove markdown code blocks
        var cleaned = content.Trim();
        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Substring(7);
        }
        if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }
        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }
        cleaned = cleaned.Trim();

        // Check for duplicate JSON objects (common LLM issue)
        var jsonObjects = new List<string>();
        var braceCount = 0;
        var startIndex = -1;
        var endIndex = -1;

        for (int i = 0; i < cleaned.Length; i++)
        {
            if (cleaned[i] == '{')
            {
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                braceCount++;
            }
            else if (cleaned[i] == '}')
            {
                braceCount--;
                if (braceCount == 0 && startIndex != -1)
                {
                    endIndex = i;
                    var jsonObject = cleaned.Substring(startIndex, endIndex - startIndex + 1);
                    jsonObjects.Add(jsonObject);
                    
                    // Reset for next object
                    startIndex = -1;
                    endIndex = -1;
                }
            }
        }

        // If we found multiple JSON objects, check if they're duplicates
        if (jsonObjects.Count > 1)
        {
            // Check if all objects are identical (common LLM duplication issue)
            var firstObject = jsonObjects[0];
            var allIdentical = jsonObjects.All(obj => obj == firstObject);
            
            if (allIdentical)
            {
                // Return the first object (they're all the same)
                return firstObject;
            }
            
            // If they're different, log a warning and return the first one
            Console.WriteLine($"Warning: Found {jsonObjects.Count} different JSON objects in LLM response. Using the first one.");
            return firstObject;
        }
        else if (jsonObjects.Count == 1)
        {
            return jsonObjects[0];
        }

        // Fallback: try to find just the first complete JSON object
        braceCount = 0;
        startIndex = -1;
        endIndex = -1;

        for (int i = 0; i < cleaned.Length; i++)
        {
            if (cleaned[i] == '{')
            {
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                braceCount++;
            }
            else if (cleaned[i] == '}')
            {
                braceCount--;
                if (braceCount == 0 && startIndex != -1)
                {
                    endIndex = i;
                    break;
                }
            }
        }

        if (startIndex != -1 && endIndex != -1)
        {
            return cleaned.Substring(startIndex, endIndex - startIndex + 1);
        }

        return cleaned;
    }
}
