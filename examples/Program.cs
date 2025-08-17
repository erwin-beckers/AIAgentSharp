using AIAgentSharp;

/// <summary>
///     Main program demonstrating the usage of the Agent framework.
///     This example shows how to create an agent, configure it, subscribe to events,
///     and run it with a specific goal and tools.
/// </summary>
internal class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments. Use --memory to use in-memory state store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when OPENAI_API_KEY environment variable is not set.</exception>
    private static async Task Main(string[] args)
    {
        // Get OpenAI API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Set OPENAI_API_KEY env var.");

        // Determine whether to use memory store or file store
        var useMemoryStore = args.Contains("--memory") || Environment.GetEnvironmentVariable("USE_MEMORY_STORE")?.ToLower() == "true";

        // Create the appropriate state store
        IAgentStateStore store = useMemoryStore ? new MemoryAgentStateStore() : new FileAgentStateStore("./agent_state");

        // Create LLM client and tools
        ILlmClient llm = new OpenAiLlmClient(apiKey);
        var tools = new List<ITool> { new ConcatTool(), new GetIndicatorTool() };

        // Configure the agent
        var config = new AgentConfiguration
        {
            MaxTurns = 40,
            UseFunctionCalling = true,
            EmitPublicStatus = true // Enable public status updates
        };
        var agent = new AIAgentSharp.AIAgentSharp(llm, store, config: config);

        // Subscribe to events for real-time monitoring
        agent.RunStarted += (sender, e) => Console.WriteLine($"[EVENT] Run started for {e.AgentId} with goal: {e.Goal}");
        agent.StepStarted += (sender, e) => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} started for {e.AgentId}");
        agent.LlmCallStarted += (sender, e) => Console.WriteLine($"[EVENT] LLM call started for {e.AgentId} turn {e.TurnIndex + 1}");
        agent.ToolCallStarted += (sender, e) => Console.WriteLine($"[EVENT] Tool call started: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}");
        agent.StepCompleted += (sender, e) => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} completed for {e.AgentId} - Continue: {e.Continue}, Tool: {e.ExecutedTool}");
        agent.RunCompleted += (sender, e) => Console.WriteLine($"[EVENT] Run completed for {e.AgentId} - Success: {e.Succeeded}, Turns: {e.TotalTurns}");

        // Subscribe to public status updates
        agent.StatusUpdate += (sender, e) =>
        {
            Console.WriteLine();
            Console.WriteLine($"🔄 STATUS UPDATE (Turn {e.TurnIndex + 1}): {e.StatusTitle}");

            if (!string.IsNullOrEmpty(e.StatusDetails))
            {
                Console.WriteLine($"   Details: {e.StatusDetails}");
            }

            if (!string.IsNullOrEmpty(e.NextStepHint))
            {
                Console.WriteLine($"   Next: {e.NextStepHint}");
            }

            if (e.ProgressPct.HasValue)
            {
                Console.WriteLine($"   Progress: {e.ProgressPct}%");
            }

            Console.WriteLine();
        };

        // Subscribe to LLM call completion events
        agent.LlmCallCompleted += (sender, e) =>
        {
            if (e.Error != null)
            {
                Console.WriteLine($"[EVENT] LLM call failed for {e.AgentId} turn {e.TurnIndex + 1}: {e.Error}");
            }
            else
            {
                Console.WriteLine($"[EVENT] LLM call completed for {e.AgentId} turn {e.TurnIndex + 1} - Action: {e.LlmMessage?.Action}");
            }
        };

        // Subscribe to tool call completion events
        agent.ToolCallCompleted += (sender, e) =>
        {
            if (e.Success)
            {
                Console.WriteLine($"[EVENT] Tool call completed: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1} in {e.ExecutionTime.TotalMilliseconds:F0}ms");
            }
            else
            {
                Console.WriteLine($"[EVENT] Tool call failed: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}: {e.Error}");
            }
        };

        // Define the goal for the agent
        var goal = "Draft a minimal risk plan for MNQ using RSI (14) and ATR (14). " +
                   "First fetch the current RSI and ATR values using the get_indicator tool, then provide a risk assessment.";

        // Run the agent
        var result = await agent.RunAsync("demo-session-1", goal, tools);

        // Display the final results
        Console.WriteLine("=== RUN RESULT ===");
        Console.WriteLine($"Succeeded: {result.Succeeded}");
        Console.WriteLine($"Error:     {result.Error}");
        Console.WriteLine($"Final:     {result.FinalOutput}");
    }
}