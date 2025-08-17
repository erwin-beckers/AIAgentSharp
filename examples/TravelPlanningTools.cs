using AIAgentSharp;
using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp.Examples;

// Parameter classes for travel planning tools
public class SearchFlightsParams
{
    [Required]
    public string Origin { get; set; } = string.Empty;

    [Required]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public string DepartureDate { get; set; } = string.Empty;

    public string? ReturnDate { get; set; }

    public int Passengers { get; set; } = 1;
}

public class SearchHotelsParams
{
    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string CheckInDate { get; set; } = string.Empty;

    [Required]
    public string CheckOutDate { get; set; } = string.Empty;

    public int Guests { get; set; } = 2;

    public int? MaxPricePerNight { get; set; }
}

public class SearchAttractionsParams
{
    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public int? MaxPrice { get; set; }
}

public class CalculateTripCostParams
{
    public double FlightCost { get; set; }
    public double HotelCost { get; set; }
    public double ActivitiesCost { get; set; }
    public double DailyFoodBudget { get; set; } = 50;
    public int TripDurationDays { get; set; }
    public int NumberOfTravelers { get; set; } = 2;
}

/// <summary>
/// Tool for searching available flights with pricing information.
/// </summary>
public class SearchFlightsTool : BaseTool<SearchFlightsParams, object>
{
    public override string Name => "search_flights";
    public override string Description => "Search for available flights between airports with pricing information";

    protected override async Task<object> InvokeTypedAsync(SearchFlightsParams parameters, CancellationToken cancellationToken = default)
    {
        // Simulate API delay
        await Task.Delay(100, cancellationToken);

        // Mock flight data
        var flights = new List<object>
        {
            new
            {
                airline = "Air France",
                flight_number = "AF123",
                departure_time = "09:30",
                arrival_time = "22:15",
                duration = "7h 45m",
                price_per_person = 450,
                total_price = 450 * parameters.Passengers,
                stops = 0,
                class_type = "Economy"
            },
            new
            {
                airline = "Delta Airlines",
                flight_number = "DL456",
                departure_time = "14:20",
                arrival_time = "03:10",
                duration = "6h 50m",
                price_per_person = 380,
                total_price = 380 * parameters.Passengers,
                stops = 1,
                class_type = "Economy"
            },
            new
            {
                airline = "British Airways",
                flight_number = "BA789",
                departure_time = "18:45",
                arrival_time = "08:30",
                duration = "7h 45m",
                price_per_person = 520,
                total_price = 520 * parameters.Passengers,
                stops = 0,
                class_type = "Economy"
            }
        };

        return new
        {
            search_criteria = new
            {
                origin = parameters.Origin,
                destination = parameters.Destination,
                departure_date = parameters.DepartureDate,
                return_date = parameters.ReturnDate,
                passengers = parameters.Passengers
            },
            available_flights = flights,
            search_summary = $"Found {flights.Count} flights from {parameters.Origin} to {parameters.Destination} on {parameters.DepartureDate}"
        };
    }
}

/// <summary>
/// Tool for searching hotel options with ratings and pricing.
/// </summary>
public class SearchHotelsTool : BaseTool<SearchHotelsParams, object>
{
    public override string Name => "search_hotels";
    public override string Description => "Search for hotel options in a specific city with ratings, amenities, and pricing";

    protected override async Task<object> InvokeTypedAsync(SearchHotelsParams parameters, CancellationToken cancellationToken = default)
    {
        // Simulate API delay
        await Task.Delay(150, cancellationToken);

        // Mock hotel data
        var hotels = new List<object>
        {
            new
            {
                name = "Hotel Le Grand",
                rating = 4.5,
                price_per_night = 180,
                total_price = 180 * GetNights(parameters.CheckInDate, parameters.CheckOutDate),
                location = "City Center",
                amenities = new[] { "WiFi", "Breakfast", "Gym", "Spa" },
                room_type = "Deluxe Double",
                available = true
            },
            new
            {
                name = "Paris Boutique Hotel",
                rating = 4.2,
                price_per_night = 120,
                total_price = 120 * GetNights(parameters.CheckInDate, parameters.CheckOutDate),
                location = "Montmartre",
                amenities = new[] { "WiFi", "Breakfast", "Terrace" },
                room_type = "Standard Double",
                available = true
            },
            new
            {
                name = "Luxury Palace Hotel",
                rating = 4.8,
                price_per_night = 350,
                total_price = 350 * GetNights(parameters.CheckInDate, parameters.CheckOutDate),
                location = "Champs-Élysées",
                amenities = new[] { "WiFi", "Breakfast", "Pool", "Spa", "Restaurant", "Concierge" },
                room_type = "Suite",
                available = true
            }
        };

        // Filter by max price if specified
        if (parameters.MaxPricePerNight.HasValue)
        {
            hotels = hotels.Where(h => (int)h.GetType().GetProperty("price_per_night")!.GetValue(h)! <= parameters.MaxPricePerNight.Value).ToList();
        }

        return new
        {
            search_criteria = new
            {
                city = parameters.City,
                check_in = parameters.CheckInDate,
                check_out = parameters.CheckOutDate,
                guests = parameters.Guests,
                max_price = parameters.MaxPricePerNight
            },
            available_hotels = hotels,
            search_summary = $"Found {hotels.Count} hotels in {parameters.City} for {GetNights(parameters.CheckInDate, parameters.CheckOutDate)} nights"
        };
    }

    private static int GetNights(string checkIn, string checkOut)
    {
        if (DateTime.TryParse(checkIn, out var checkInDate) && DateTime.TryParse(checkOut, out var checkOutDate))
        {
            return (int)(checkOutDate - checkInDate).TotalDays;
        }
        return 1;
    }
}

/// <summary>
/// Tool for searching tourist attractions and activities.
/// </summary>
public class SearchAttractionsTool : BaseTool<SearchAttractionsParams, object>
{
    public override string Name => "search_attractions";
    public override string Description => "Search for tourist attractions, museums, restaurants, and activities in a city";

    protected override async Task<object> InvokeTypedAsync(SearchAttractionsParams parameters, CancellationToken cancellationToken = default)
    {
        // Simulate API delay
        await Task.Delay(80, cancellationToken);

        // Mock attraction data based on category
        var attractions = parameters.Category.ToLower() switch
        {
            "museums" => new List<object>
            {
                new { name = "Louvre Museum", price = 17, rating = 4.7, duration = "3-4 hours", type = "Museum", highlights = new[] { "Mona Lisa", "Venus de Milo", "Egyptian Antiquities" } },
                new { name = "Musée d'Orsay", price = 16, rating = 4.6, duration = "2-3 hours", type = "Museum", highlights = new[] { "Impressionist Art", "Van Gogh Collection", "Art Nouveau" } },
                new { name = "Centre Pompidou", price = 15, rating = 4.4, duration = "2-3 hours", type = "Museum", highlights = new[] { "Modern Art", "Contemporary Exhibitions", "Architecture" } }
            },
            "landmarks" => new List<object>
            {
                new { name = "Eiffel Tower", price = 26, rating = 4.5, duration = "2-3 hours", type = "Landmark", highlights = new[] { "City Views", "Light Show", "Restaurants" } },
                new { name = "Arc de Triomphe", price = 13, rating = 4.3, duration = "1-2 hours", type = "Landmark", highlights = new[] { "Historical Monument", "City Views", "Champs-Élysées" } },
                new { name = "Notre-Dame Cathedral", price = 0, rating = 4.8, duration = "1-2 hours", type = "Landmark", highlights = new[] { "Gothic Architecture", "Religious History", "Free Entry" } }
            },
            "restaurants" => new List<object>
            {
                new { name = "Le Comptoir du Relais", price = 45, rating = 4.6, duration = "1-2 hours", type = "Restaurant", cuisine = "French Bistro", highlights = new[] { "Traditional French", "Wine Selection", "Local Favorite" } },
                new { name = "L'Arpège", price = 350, rating = 4.9, duration = "2-3 hours", type = "Restaurant", cuisine = "Fine Dining", highlights = new[] { "Michelin Star", "Organic Ingredients", "Tasting Menu" } },
                new { name = "Chez L'Ami Louis", price = 85, rating = 4.4, duration = "1-2 hours", type = "Restaurant", cuisine = "Traditional French", highlights = new[] { "Classic Dishes", "Historic Setting", "Authentic Experience" } }
            },
            "activities" => new List<object>
            {
                new { name = "Seine River Cruise", price = 15, rating = 4.4, duration = "1 hour", type = "Activity", highlights = new[] { "City Views", "Historical Commentary", "Evening Option" } },
                new { name = "Walking Tour - Historic Paris", price = 25, rating = 4.6, duration = "3 hours", type = "Activity", highlights = new[] { "Local Guide", "Hidden Gems", "Historical Stories" } },
                new { name = "Cooking Class - French Pastries", price = 75, rating = 4.8, duration = "3 hours", type = "Activity", highlights = new[] { "Hands-on Experience", "Recipe Book", "Tasting Session" } }
            },
            _ => new List<object>
            {
                new { name = "Popular Attractions", price = 0, rating = 4.5, duration = "Varies", type = "General", highlights = new[] { "Multiple Options", "Various Prices", "Different Categories" } }
            }
        };

        // Filter by max price if specified
        if (parameters.MaxPrice.HasValue)
        {
            attractions = attractions.Where(a => (int)a.GetType().GetProperty("price")!.GetValue(a)! <= parameters.MaxPrice.Value).ToList();
        }

        return new
        {
            search_criteria = new
            {
                city = parameters.City,
                category = parameters.Category,
                max_price = parameters.MaxPrice
            },
            attractions = attractions,
            search_summary = $"Found {attractions.Count} {parameters.Category} in {parameters.City}"
        };
    }
}

/// <summary>
/// Tool for calculating total trip cost and creating budget summary.
/// </summary>
public class CalculateTripCostTool : BaseTool<CalculateTripCostParams, object>
{
    public override string Name => "calculate_trip_cost";
    public override string Description => "Calculate the total cost of a trip including flights, hotels, and activities";

    protected override async Task<object> InvokeTypedAsync(CalculateTripCostParams parameters, CancellationToken cancellationToken = default)
    {
        // Simulate calculation delay
        await Task.Delay(50, cancellationToken);

        var foodCost = parameters.DailyFoodBudget * parameters.TripDurationDays * parameters.NumberOfTravelers;
        var totalCost = parameters.FlightCost + parameters.HotelCost + parameters.ActivitiesCost + foodCost;
        var costPerPerson = totalCost / parameters.NumberOfTravelers;

        return new
        {
            cost_breakdown = new
            {
                flights = parameters.FlightCost,
                hotel = parameters.HotelCost,
                activities = parameters.ActivitiesCost,
                food = foodCost,
                total = totalCost
            },
            summary = new
            {
                total_cost = totalCost,
                cost_per_person = costPerPerson,
                trip_duration = $"{parameters.TripDurationDays} days",
                travelers = parameters.NumberOfTravelers,
                daily_food_budget = parameters.DailyFoodBudget
            },
            recommendations = new
            {
                budget_status = totalCost <= 2000 ? "Within budget" : "Over budget",
                savings_tips = totalCost > 2000 ? new[] { "Consider cheaper hotels", "Look for free activities", "Reduce dining budget" } : new[] { "Budget looks good!" }
            }
        };
    }
}
