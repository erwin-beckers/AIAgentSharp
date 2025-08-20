# Travel Planning Example

This example demonstrates how to build a comprehensive travel planning agent using AIAgentSharp. The agent will help users plan trips by researching destinations, finding flights, booking accommodations, and creating detailed itineraries.

## Overview

The travel planning agent showcases several key features of AIAgentSharp:
- Multi-step reasoning with Chain of Thought
- Tool integration for external APIs
- State management for complex workflows
- Event monitoring for real-time updates
- Error handling and retry logic

## Prerequisites

- AIAgentSharp installed and configured
- API keys for travel services (optional, for real integrations)
- Basic understanding of the agent framework

## Implementation

### 1. Travel Planning Agent Class

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Tools;
using AIAgentSharp.Reasoning;
using AIAgentSharp.Events;
using AIAgentSharp.State;

public class TravelPlanningAgent : Agent
{
    private readonly ITravelSearchTool _travelSearchTool;
    private readonly IBookingTool _bookingTool;
    private readonly IWeatherTool _weatherTool;
    
    public TravelPlanningAgent(
        ILLMClient llmClient,
        ITravelSearchTool travelSearchTool,
        IBookingTool bookingTool,
        IWeatherTool weatherTool,
        IAgentStateManager stateManager,
        IEventManager eventManager)
        : base(llmClient, stateManager, eventManager)
    {
        _travelSearchTool = travelSearchTool;
        _bookingTool = bookingTool;
        _weatherTool = weatherTool;
        
        // Configure reasoning engine
        ReasoningEngine = new ChainOfThoughtReasoningEngine();
        
        // Register tools
        RegisterTool(_travelSearchTool);
        RegisterTool(_bookingTool);
        RegisterTool(_weatherTool);
        
        // Set up event handlers
        EventManager.Subscribe<AgentStepCompletedEvent>(OnStepCompleted);
        EventManager.Subscribe<ToolExecutionEvent>(OnToolExecuted);
    }
    
    public async Task<TravelPlan> PlanTripAsync(TripRequest request)
    {
        var context = new TravelPlanningContext
        {
            Request = request,
            Status = PlanningStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        
        // Save initial state
        await StateManager.SaveStateAsync("travel_planning", context);
        
        var prompt = GeneratePlanningPrompt(request);
        var response = await ExecuteAsync(prompt);
        
        // Parse and validate the response
        var plan = ParseTravelPlan(response);
        
        // Update final state
        context.Status = PlanningStatus.Completed;
        context.Plan = plan;
        await StateManager.SaveStateAsync("travel_planning", context);
        
        return plan;
    }
    
    private string GeneratePlanningPrompt(TripRequest request)
    {
        return $@"
You are a professional travel planner. Create a comprehensive travel plan for the following request:

Destination: {request.Destination}
Dates: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}
Budget: ${request.Budget}
Travelers: {request.NumberOfTravelers}
Preferences: {string.Join(", ", request.Preferences)}

Please follow these steps:
1. Research the destination and identify key attractions
2. Check weather conditions for the travel dates
3. Search for available flights within budget
4. Find suitable accommodations
5. Create a detailed day-by-day itinerary
6. Calculate total estimated costs
7. Provide travel tips and recommendations

Use the available tools to gather real information and create a practical, detailed plan.
";
    }
    
    private TravelPlan ParseTravelPlan(string response)
    {
        // Implementation to parse the LLM response into a structured TravelPlan
        // This would include JSON parsing and validation
        return new TravelPlan();
    }
    
    private void OnStepCompleted(AgentStepCompletedEvent e)
    {
        Console.WriteLine($"Step completed: {e.StepNumber} - {e.Description}");
    }
    
    private void OnToolExecuted(ToolExecutionEvent e)
    {
        Console.WriteLine($"Tool executed: {e.ToolName} - Duration: {e.Duration}ms");
    }
}
```

### 2. Travel Planning Context and Models

```csharp
public class TravelPlanningContext
{
    public TripRequest Request { get; set; }
    public TravelPlan Plan { get; set; }
    public PlanningStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Steps { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TripRequest
{
    public string Destination { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public int NumberOfTravelers { get; set; }
    public List<string> Preferences { get; set; } = new();
}

public class TravelPlan
{
    public string Destination { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<FlightOption> Flights { get; set; } = new();
    public List<AccommodationOption> Accommodations { get; set; } = new();
    public List<DayItinerary> Itinerary { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public List<string> TravelTips { get; set; } = new();
    public WeatherForecast Weather { get; set; }
}

public enum PlanningStatus
{
    InProgress,
    Completed,
    Failed
}
```

### 3. Travel Tools Implementation

```csharp
[Tool("search_destination")]
public class DestinationSearchTool : ITool
{
    public string Name => "search_destination";
    public string Description => "Search for information about a travel destination";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        var destination = parameters.GetString("destination");
        
        // Simulate API call to travel information service
        var info = await GetDestinationInfoAsync(destination);
        
        return new ToolResult
        {
            Success = true,
            Data = new
            {
                attractions = info.Attractions,
                culture = info.Culture,
                cuisine = info.Cuisine,
                transportation = info.Transportation,
                safety = info.Safety
            }
        };
    }
}

[Tool("search_flights")]
public class FlightSearchTool : ITool
{
    public string Name => "search_flights";
    public string Description => "Search for available flights between airports";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        var from = parameters.GetString("from");
        var to = parameters.GetString("to");
        var date = parameters.GetDateTime("date");
        var passengers = parameters.GetInt("passengers");
        var maxPrice = parameters.GetDecimal("max_price");
        
        // Simulate flight search API call
        var flights = await SearchFlightsAsync(from, to, date, passengers, maxPrice);
        
        return new ToolResult
        {
            Success = true,
            Data = flights
        };
    }
}

[Tool("search_accommodations")]
public class AccommodationSearchTool : ITool
{
    public string Name => "search_accommodations";
    public string Description => "Search for available accommodations in a destination";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        var destination = parameters.GetString("destination");
        var checkIn = parameters.GetDateTime("check_in");
        var checkOut = parameters.GetDateTime("check_out");
        var guests = parameters.GetInt("guests");
        var maxPrice = parameters.GetDecimal("max_price_per_night");
        
        // Simulate accommodation search API call
        var accommodations = await SearchAccommodationsAsync(destination, checkIn, checkOut, guests, maxPrice);
        
        return new ToolResult
        {
            Success = true,
            Data = accommodations
        };
    }
}

[Tool("get_weather")]
public class WeatherTool : ITool
{
    public string Name => "get_weather";
    public string Description => "Get weather forecast for a destination";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        var destination = parameters.GetString("destination");
        var date = parameters.GetDateTime("date");
        
        // Simulate weather API call
        var weather = await GetWeatherForecastAsync(destination, date);
        
        return new ToolResult
        {
            Success = true,
            Data = weather
        };
    }
}
```

### 4. Usage Example

```csharp
// Setup and configuration
var services = new ServiceCollection();

// Register LLM client
services.AddSingleton<ILLMClient>(new OpenAILLMClient(apiKey));

// Register tools
services.AddSingleton<ITravelSearchTool, DestinationSearchTool>();
services.AddSingleton<IBookingTool, BookingTool>();
services.AddSingleton<IWeatherTool, WeatherTool>();

// Register state and event managers
services.AddSingleton<IAgentStateManager, FileStateManager>();
services.AddSingleton<IEventManager, EventManager>();

var serviceProvider = services.BuildServiceProvider();

// Create the travel planning agent
var agent = new TravelPlanningAgent(
    serviceProvider.GetRequiredService<ILLMClient>(),
    serviceProvider.GetRequiredService<ITravelSearchTool>(),
    serviceProvider.GetRequiredService<IBookingTool>(),
    serviceProvider.GetRequiredService<IWeatherTool>(),
    serviceProvider.GetRequiredService<IAgentStateManager>(),
    serviceProvider.GetRequiredService<IEventManager>()
);

// Create a trip request
var request = new TripRequest
{
    Destination = "Tokyo, Japan",
    StartDate = DateTime.Now.AddDays(30),
    EndDate = DateTime.Now.AddDays(37),
    Budget = 5000,
    NumberOfTravelers = 2,
    Preferences = new List<string> { "culture", "food", "shopping" }
};

// Plan the trip
var travelPlan = await agent.PlanTripAsync(request);

// Display the results
Console.WriteLine($"Travel Plan for {travelPlan.Destination}");
Console.WriteLine($"Total Cost: ${travelPlan.TotalEstimatedCost}");
Console.WriteLine($"Duration: {travelPlan.EndDate - travelPlan.StartDate} days");

foreach (var day in travelPlan.Itinerary)
{
    Console.WriteLine($"\nDay {day.DayNumber}: {day.Title}");
    foreach (var activity in day.Activities)
    {
        Console.WriteLine($"  - {activity.Time}: {activity.Description}");
    }
}
```

## Key Features Demonstrated

### 1. Multi-Step Reasoning
The agent uses Chain of Thought reasoning to break down the complex task of travel planning into logical steps:
- Destination research
- Weather checking
- Flight search
- Accommodation booking
- Itinerary creation
- Cost calculation

### 2. Tool Integration
The example shows how to integrate multiple external tools:
- Destination search for attractions and information
- Flight search for transportation options
- Accommodation search for lodging
- Weather service for planning considerations

### 3. State Management
The agent maintains state throughout the planning process:
- Saves progress at each step
- Tracks planning status
- Stores metadata and intermediate results
- Enables resuming interrupted planning sessions

### 4. Event Monitoring
Real-time monitoring of the planning process:
- Step completion events
- Tool execution events
- Performance metrics
- Error tracking

### 5. Error Handling
Robust error handling for external API calls:
- Retry logic for failed requests
- Fallback options for unavailable services
- Graceful degradation when tools fail

## Advanced Features

### Custom Reasoning Strategies
You can extend the agent with custom reasoning strategies:

```csharp
public class TravelPlanningReasoningEngine : IReasoningEngine
{
    public async Task<ReasoningResult> ReasonAsync(string prompt, List<ITool> tools)
    {
        // Custom reasoning logic specific to travel planning
        // Could include budget optimization, preference matching, etc.
    }
}
```

### Integration with Real APIs
Replace the simulated tools with real API integrations:

```csharp
public class RealFlightSearchTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        // Real API call to flight search service
        var response = await _httpClient.GetAsync($"flights?from={from}&to={to}&date={date}");
        // Parse and return real flight data
    }
}
```

## Best Practices

1. **Error Handling**: Always implement proper error handling for external API calls
2. **Rate Limiting**: Respect API rate limits and implement appropriate delays
3. **Caching**: Cache frequently requested data to improve performance
4. **Validation**: Validate all inputs and outputs from tools
5. **Monitoring**: Use events to monitor performance and identify bottlenecks
6. **Testing**: Create comprehensive tests for each tool and the overall workflow

## Next Steps

- Implement real API integrations for travel services
- Add more sophisticated reasoning strategies
- Create a web interface for the travel planning agent
- Add support for group travel planning
- Implement budget optimization algorithms
- Add travel insurance and safety recommendations

This example demonstrates how AIAgentSharp can be used to build sophisticated, real-world applications that combine AI reasoning with external data sources and APIs.
