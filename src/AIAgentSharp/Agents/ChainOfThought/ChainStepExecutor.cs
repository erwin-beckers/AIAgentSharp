using AIAgentSharp.Agents.Interfaces;
using System.Linq;

namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Executes individual Chain of Thought reasoning steps.
/// </summary>
public sealed class ChainStepExecutor
{
    private readonly IChainPromptBuilder _promptBuilder;
    private readonly ILlmCommunicator _llmCommunicator;
    private readonly IStatusManager _statusManager;

    public ChainStepExecutor(
        IChainPromptBuilder promptBuilder,
        ILlmCommunicator llmCommunicator,
        IStatusManager statusManager)
    {
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _llmCommunicator = llmCommunicator ?? throw new ArgumentNullException(nameof(llmCommunicator));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    /// <summary>
    /// Performs the analysis step of Chain of Thought reasoning.
    /// </summary>
    public async Task<ChainStepExecutionResult> PerformAnalysisStepAsync(
        string goal, 
        string context, 
        IDictionary<string, ITool> tools, 
        CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Analyzing problem", "Breaking down the goal into components", "Understanding requirements", null);

        var prompt = _promptBuilder.BuildAnalysisPrompt(goal, context, tools);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "reasoning", 0, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = "Empty LLM response"
            };
        }

        try
        {
            var result = JsonUtil.ParseChainOfThoughtResponse(content);
            return new ChainStepExecutionResult
            {
                Success = true,
                Reasoning = result.Reasoning ?? "",
                Confidence = result.ReasoningConfidence ?? 0.5,
                Insights = result.Insights ?? new List<string>(),
                StepType = ReasoningStepType.Analysis,
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = $"Failed to parse LLM response: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Performs the planning step of Chain of Thought reasoning.
    /// </summary>
    public async Task<ChainStepExecutionResult> PerformPlanningStepAsync(
        string goal, 
        string context, 
        IDictionary<string, ITool> tools, 
        List<string> analysisInsights, 
        CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Planning approach", "Developing solution strategy", "Creating execution plan", null);

        var prompt = _promptBuilder.BuildPlanningPrompt(goal, context, tools, analysisInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "reasoning", 1, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = "Empty LLM response"
            };
        }

        try
        {
            var result = JsonUtil.ParseChainOfThoughtResponse(content);
            return new ChainStepExecutionResult
            {
                Success = true,
                Reasoning = result.Reasoning ?? "",
                Confidence = result.ReasoningConfidence ?? 0.5,
                Insights = result.Insights ?? new List<string>(),
                StepType = ReasoningStepType.Planning,
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = $"Failed to parse LLM response: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Performs the strategy step of Chain of Thought reasoning.
    /// </summary>
    public async Task<ChainStepExecutionResult> PerformStrategyStepAsync(
        string goal, 
        string context, 
        IDictionary<string, ITool> tools, 
        List<string> planningInsights, 
        CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Developing strategy", "Determining execution approach", "Selecting optimal path", null);

        var prompt = _promptBuilder.BuildStrategyPrompt(goal, context, tools, planningInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "reasoning", 2, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = "Empty LLM response"
            };
        }

        try
        {
            var result = JsonUtil.ParseChainOfThoughtResponse(content);
            return new ChainStepExecutionResult
            {
                Success = true,
                Reasoning = result.Reasoning ?? "",
                Confidence = result.ReasoningConfidence ?? 0.5,
                Insights = result.Insights ?? new List<string>(),
                StepType = ReasoningStepType.Decision,
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            return new ChainStepExecutionResult
            {
                Success = false,
                Error = $"Failed to parse LLM response: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Performs the evaluation step of Chain of Thought reasoning.
    /// </summary>
    public async Task<ChainEvaluationExecutionResult> PerformEvaluationStepAsync(
        string goal, 
        string context, 
        IDictionary<string, ITool> tools, 
        List<string> allInsights, 
        CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Evaluating solution", "Assessing approach quality", "Finalizing decision", null);

        var prompt = _promptBuilder.BuildEvaluationPrompt(goal, context, tools, allInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "reasoning", 3, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return new ChainEvaluationExecutionResult
            {
                Success = false,
                Error = "Empty LLM response"
            };
        }

        try
        {
            var result = JsonUtil.ParseChainOfThoughtResponse(content);
            return new ChainEvaluationExecutionResult
            {
                Success = true,
                Reasoning = result.Reasoning ?? "",
                Confidence = result.ReasoningConfidence ?? 0.5,
                Insights = result.Insights ?? new List<string>(),
                Conclusion = result.Conclusion ?? "",
                StepType = ReasoningStepType.Evaluation,
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            return new ChainEvaluationExecutionResult
            {
                Success = false,
                Error = $"Failed to parse LLM response: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Performs validation of the reasoning chain.
    /// </summary>
    public async Task<ChainValidationResult> PerformValidationAsync(
        string goal, 
        List<string> insights, 
        string conclusion, 
        double confidence, 
        CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Validating reasoning", "Checking logic and consistency", "Quality assurance", null);
        
        var prompt = _promptBuilder.BuildValidationPrompt(goal, insights, conclusion, confidence);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        
        // Use LlmCommunicator for proper streaming and event emission
        var content = await _llmCommunicator.CallLlmWithStreamingAsync(messages, "reasoning", 4, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return new ChainValidationResult
            {
                IsValid = false,
                Error = "Empty LLM response"
            };
        }

        try
        {
            var result = JsonUtil.ParseChainOfThoughtResponse(content);
            return new ChainValidationResult
            {
                IsValid = result.IsValid ?? false,
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            return new ChainValidationResult
            {
                IsValid = false,
                Error = $"Failed to parse LLM response: {ex.Message}"
            };
        }
    }
}