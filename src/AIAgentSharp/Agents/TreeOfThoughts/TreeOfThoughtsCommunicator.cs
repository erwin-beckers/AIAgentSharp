using AIAgentSharp.Agents.Interfaces;
using System.Diagnostics;

namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Handles LLM communication for Tree of Thoughts reasoning using the unified LlmCommunicator.
/// </summary>
internal sealed class TreeOfThoughtsCommunicator
{
    private readonly ILlmCommunicator _llmCommunicator;

    public TreeOfThoughtsCommunicator(ILlmCommunicator llmCommunicator)
    {
        _llmCommunicator = llmCommunicator ?? throw new ArgumentNullException(nameof(llmCommunicator));
    }

    /// <summary>
    /// Calls the LLM and parses the response specifically for Tree of Thoughts format.
    /// </summary>
    private async Task<ModelMessage?> CallLlmForTreeOfThoughtsAsync(IEnumerable<LlmMessage> messages, CancellationToken cancellationToken)
    {
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "tree-of-thoughts", 0, cancellationToken);
        
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        try
        {
            return JsonUtil.ParseTreeOfThoughtsResponse(content);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw, let the caller handle it
            Trace.WriteLine($"Failed to parse Tree of Thoughts response: {ex.Message}");
            Console.WriteLine($"Failed to parse Tree of Thoughts response: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates the initial root thought for the tree.
    /// </summary>
    public async Task<string> GenerateRootThoughtAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        
        var prompt = $@"You are starting a Tree of Thoughts exploration for a complex problem.

GOAL: {goal}
CONTEXT: {context}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Generate an initial thought or hypothesis about how to approach this goal. This should be a high-level starting point that can branch into multiple directions.

Provide your initial thought in the following JSON format:
{{
  ""thought"": ""Your initial thought or hypothesis here..."",
  ""thought_type"": ""Hypothesis""
}}

Focus on creating a thought that opens up multiple exploration paths.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        var result = await CallLlmForTreeOfThoughtsAsync(messages, cancellationToken);

        if (result == null || string.IsNullOrWhiteSpace(result.Thought))
        {
            throw new FormatException("Failed to generate root thought from LLM response");
        }

        return result.Thought;
    }

    /// <summary>
    /// Generates child thoughts from a parent node.
    /// </summary>
    public async Task<List<TreeThoughtGenerator.ChildThought>> GenerateChildThoughtsAsync(ThoughtNode parentNode, CancellationToken cancellationToken)
    {
        var prompt = $@"You are generating child thoughts in a Tree of Thoughts exploration.

PARENT THOUGHT: {parentNode.Thought}
PARENT DEPTH: {parentNode.Depth}
PARENT SCORE: {parentNode.Score:F2}

TASK: Generate 2-3 child thoughts that explore different aspects or directions from the parent thought. Each child should represent a different approach, alternative, or next step.

Provide your child thoughts in the following JSON format:
{{
  ""children"": [
    {{
      ""thought"": ""First child thought..."",
      ""thought_type"": ""Analysis"",
      ""estimated_score"": 0.75
    }},
    {{
      ""thought"": ""Second child thought..."",
      ""thought_type"": ""Alternative"",
      ""estimated_score"": 0.65
    }}
  ]
}}

Focus on creating diverse, meaningful child thoughts that expand the exploration space.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        var result = await CallLlmForTreeOfThoughtsAsync(messages, cancellationToken);

        if (result == null || result.Children == null)
        {
            return new List<TreeThoughtGenerator.ChildThought>();
        }

        var childThoughts = new List<TreeThoughtGenerator.ChildThought>();
        foreach (var child in result.Children)
        {
            if (child is System.Text.Json.JsonElement childElement)
            {
                try
                {
                    var thought = childElement.TryGetProperty("thought", out var thoughtProp) ? thoughtProp.GetString() ?? "" : "";
                    var thoughtTypeStr = childElement.TryGetProperty("thought_type", out var typeProp) ? typeProp.GetString() ?? "Hypothesis" : "Hypothesis";
                    var estimatedScore = childElement.TryGetProperty("estimated_score", out var scoreProp) ? scoreProp.GetDouble() : 0.5;

                    if (!string.IsNullOrWhiteSpace(thought))
                    {
                        // Try to parse the thought type as enum, fallback to Hypothesis
                        var thoughtType = Enum.TryParse<ThoughtType>(thoughtTypeStr, true, out var parsedType) ? parsedType : ThoughtType.Hypothesis;
                        
                        childThoughts.Add(new TreeThoughtGenerator.ChildThought
                        {
                            Thought = thought,
                            ThoughtType = thoughtType,
                            EstimatedScore = estimatedScore
                        });
                    }
                }
                catch
                {
                    // Skip malformed child thoughts
                    continue;
                }
            }
        }

        return childThoughts;
    }

    /// <summary>
    /// Evaluates a thought node using LLM.
    /// </summary>
    public async Task<double> EvaluateThoughtNodeAsync(ThoughtNode node, CancellationToken cancellationToken)
    {
        var prompt = $@"You are evaluating a thought in a Tree of Thoughts exploration.

THOUGHT: {node.Thought}
THOUGHT TYPE: {node.ThoughtType}
DEPTH: {node.Depth}

TASK: Evaluate the quality and potential of this thought on a scale from 0.0 to 1.0. Consider:
1. Relevance to the goal
2. Logical soundness
3. Potential for leading to a solution
4. Novelty and creativity
5. Feasibility

Provide your evaluation in the following JSON format:
{{
  ""score"": 0.85,
  ""reasoning"": ""Brief explanation of the score""
}}

Be objective and thorough in your evaluation.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        var result = await CallLlmForTreeOfThoughtsAsync(messages, cancellationToken);

        if (result == null || !result.Score.HasValue)
        {
            return 0.5; // Default score if parsing fails
        }

        return result.Score.Value;
    }

    /// <summary>
    /// Generates a conclusion from the best path found in the tree.
    /// </summary>
    public async Task<string> GenerateConclusionFromPathAsync(List<string> bestPath, string goal, string context, IDictionary<string, ITool> tools, ReasoningTree tree, CancellationToken cancellationToken)
    {
        if (bestPath.Count == 0)
            return "No viable solution path found.";

        var pathThoughts = bestPath.Select(id => tree.Nodes[id].Thought).ToList();
        var pathText = string.Join("\n", pathThoughts.Select((thought, i) => $"Step {i + 1}: {thought}"));

        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        
        var prompt = $@"You are synthesizing a conclusion from the best path found in a Tree of Thoughts exploration.

GOAL: {goal}
CONTEXT: {context}

BEST PATH THOUGHTS:
{pathText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Based on the best path of thoughts, provide a clear, actionable conclusion that summarizes the approach and next steps.

Provide your conclusion in the following JSON format:
{{
  ""conclusion"": ""Your comprehensive conclusion and recommendation""
}}

Focus on creating a practical, actionable conclusion.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        var result = await CallLlmForTreeOfThoughtsAsync(messages, cancellationToken);

        if (result == null || string.IsNullOrWhiteSpace(result.Conclusion))
        {
            return "Failed to generate conclusion from best path.";
        }

        return result.Conclusion;
    }
}
