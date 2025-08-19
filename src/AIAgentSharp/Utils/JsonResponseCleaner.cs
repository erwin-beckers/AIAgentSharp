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

        // Find the first complete JSON object
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
