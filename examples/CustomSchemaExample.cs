using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;
using AIAgentSharp.Schema;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace example;

/// <summary>
/// Example demonstrating how to use custom schemas for complex domain-specific types.
/// This shows how to override the auto-generated schema with detailed business rules and guidance.
/// </summary>
internal class CustomSchemaExample
{
    public static async Task RunAsync(string apiKey)
    {
        var llm = new OpenAiLlmClient(apiKey);

        Console.WriteLine("Custom Schema Override Example");
        Console.WriteLine("=============================");
        Console.WriteLine();

        // Example 1: Simple custom schema for a complex type
        Console.WriteLine("Example 1: Simple custom schema for UserProfile");
        Console.WriteLine("------------------------------------------------");
        var agent1 = AIAgent.Create(llm)
            .WithTools(new List<ITool> { new CreateUserTool() })
            .WithSystemMessage("You are a user management expert. Use the provided tools to create and manage user profiles.")
            .Build();

        var goal1 = "Create a new user profile with firstName: John, lastName: Doe, email: john.doe@example.com. " +
                   "Include preferences as an array with key-value objects: {key:'language', value:'English'}, {key:'timezone', value:'America/New_York'}, {key:'emailNotifications', value:'true'}.";

        Console.WriteLine($"Goal: {goal1}");
        Console.WriteLine();

        var result1 = await agent1.RunAsync("user-creation-session-1", goal1, new List<ITool> { new CreateUserTool() });

        Console.WriteLine("=== SIMPLE USER CREATION RESULT ===");
        Console.WriteLine($"Succeeded: {result1.Succeeded}");
        if (!string.IsNullOrEmpty(result1.Error))
        {
            Console.WriteLine($"Error: {result1.Error}");
        }
        Console.WriteLine();
        Console.WriteLine("📋 RESULT:");
        Console.WriteLine(result1.FinalOutput);
        Console.WriteLine();

        // Example 2: Complex schema with detailed business rules
        Console.WriteLine("Example 2: Complex schema with detailed business rules");
        Console.WriteLine("----------------------------------------------------");
        var advancedTool = new AdvancedUserManagementTool();
        Console.WriteLine("=== DEBUG: Custom Schema for AdvancedUserManagementTool ===");
        Console.WriteLine($"Tool Name: {advancedTool.Name}");
        Console.WriteLine($"Tool Description: {advancedTool.Description}");

        // Try to get the schema from the AdvancedUserProfile class
        var profileType = typeof(AdvancedUserProfile);
        var schemaAttribute = profileType.GetCustomAttributes(typeof(ToolSchemaAttribute), true).FirstOrDefault() as ToolSchemaAttribute;
        if (schemaAttribute != null)
        {
            Console.WriteLine("Custom Schema found:");
            Console.WriteLine(schemaAttribute.Schema);
            Console.WriteLine("Additional Rules:");
            Console.WriteLine(schemaAttribute.AdditionalRules ?? "None");
        }
        else
        {
            Console.WriteLine("NO CUSTOM SCHEMA FOUND!");
        }
        Console.WriteLine("=== END DEBUG ===");

        var agent2 = AIAgent.Create(llm)
            .WithTools(new List<ITool> { advancedTool })
            .WithSystemMessage("You are an advanced user management expert. Follow the detailed rules provided in the schema.")
            .Build();

        var goal2 = "Create an advanced user profile with the following structure: " +
                   "personalInfo: {firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@company.com', dateOfBirth: '1995-03-15'}, " +
                   "securitySettings: {password: 'SecurePass123!', twoFactorEnabled: true, securityQuestions: [{question: 'What is your mother\\'s maiden name?', answer: 'Johnson'}, {question: 'What was the name of your first pet?', answer: 'Buddy'}, {question: 'What city were you born in?', answer: 'Chicago'}]}, " +
                   "preferences: {language: 'French', timezone: 'Europe/London', notifications: {email: true, sms: true, push: true}}.";

        Console.WriteLine($"Goal: {goal2}");
        Console.WriteLine();

        var result2 = await agent2.RunAsync("user-creation-session-2", goal2, new List<ITool> { new AdvancedUserManagementTool() });

        Console.WriteLine("=== ADVANCED USER CREATION RESULT ===");
        Console.WriteLine($"Succeeded: {result2.Succeeded}");
        if (!string.IsNullOrEmpty(result2.Error))
        {
            Console.WriteLine($"Error: {result2.Error}");
        }
        Console.WriteLine();
        Console.WriteLine("📋 RESULT:");
        Console.WriteLine(result2.FinalOutput);
        Console.WriteLine();

        Console.WriteLine("Both agents have been executed with custom schemas!");
        Console.WriteLine("The LLM received detailed, domain-specific schemas instead of auto-generated ones.");
        Console.WriteLine("Notice how the custom schemas provide better guidance and validation rules.");
    }
}

/// <summary>
/// Example of a simple custom schema for UserProfile.
/// This demonstrates basic schema override functionality.
/// </summary>
[ToolSchema(@"
{
  ""type"": ""object"",
  ""description"": ""Complete user profile configuration with personal information, preferences, and security settings."",
  ""properties"": {
    ""firstName"": {
      ""type"": ""string"",
      ""description"": ""User's first name"",
      ""example"": ""John""
    },
    ""lastName"": {
      ""type"": ""string"",
      ""description"": ""User's last name"",
      ""example"": ""Doe""
    },
    ""email"": {
      ""type"": ""string"",
      ""description"": ""User's email address (must be valid format)"",
      ""example"": ""john.doe@example.com""
    },
    ""preferences"": {
      ""type"": ""array"",
      ""description"": ""Array of user preferences and settings"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""key"": {
            ""type"": ""string"",
            ""description"": ""Preference key (e.g., 'language', 'timezone', 'emailNotifications')"",
            ""example"": ""language""
          },
                   ""value"": {
           ""type"": ""string"",
           ""description"": ""Preference value (always as string, even for boolean values like 'true' or 'false')"",
           ""example"": ""English""
         }
        },
        ""required"": [""key"", ""value""]
      }
    }
  },
  ""required"": [""firstName"", ""lastName"", ""email"", ""preferences""]
}")]
public class UserProfile
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public List<UserPreference> Preferences { get; set; } = new();
}

public class UserPreference
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}

/// <summary>
/// Example tool parameters with simple custom schema.
/// </summary>
public class CreateUserParams
{
    [Required]
    [Description("Complete user profile configuration with personal information, preferences, and security settings. The profile will be validated and created in the system.")]
    public UserProfile Profile { get; set; } = default!;
}

/// <summary>
/// Example tool with simple custom schema.
/// </summary>
public class CreateUserTool : BaseTool<CreateUserParams, object>
{
    public override string Name => "create_user";
    public override string Description => "Creates a new user profile in the system with the provided information and preferences.";

    protected override async Task<object> InvokeTypedAsync(CreateUserParams parameters, CancellationToken ct = default)
    {
        // Simulate user creation
        await Task.Delay(100, ct);

        return new
        {
            success = true,
            message = $"User created successfully: {parameters.Profile.FirstName} {parameters.Profile.LastName}",
            userId = Guid.NewGuid().ToString(),
            profile = parameters.Profile
        };
    }
}

/// <summary>
/// Example of a complex custom schema with detailed business rules.
/// This demonstrates advanced schema override with comprehensive guidance.
/// </summary>
[ToolSchema(@"
{
  ""type"": ""object"",
  ""description"": ""Advanced user profile configuration with comprehensive validation rules and security requirements."",
  ""properties"": {
    ""personalInfo"": {
      ""type"": ""object"",
      ""description"": ""Personal information section with strict validation rules."",
      ""properties"": {
        ""firstName"": {
          ""type"": ""string"",
          ""description"": ""User's first name (2-50 characters, letters only)"",
          ""minLength"": 2,
          ""maxLength"": 50,
          ""pattern"": ""^[a-zA-Z]+$"",
          ""example"": ""John""
        },
        ""lastName"": {
          ""type"": ""string"",
          ""description"": ""User's last name (2-50 characters, letters only)"",
          ""minLength"": 2,
          ""maxLength"": 50,
          ""pattern"": ""^[a-zA-Z]+$"",
          ""example"": ""Doe""
        },
        ""email"": {
          ""type"": ""string"",
          ""description"": ""Valid email address (required for account activation)"",
          ""format"": ""email"",
          ""example"": ""john.doe@example.com""
        },
        ""dateOfBirth"": {
          ""type"": ""string"",
          ""description"": ""Date of birth (YYYY-MM-DD format, must be 13+ years old)"",
          ""format"": ""date"",
          ""example"": ""1990-01-15""
        }
      },
      ""required"": [""firstName"", ""lastName"", ""email"", ""dateOfBirth""]
    },
    ""securitySettings"": {
      ""type"": ""object"",
      ""description"": ""Security and authentication settings with specific requirements."",
      ""properties"": {
        ""password"": {
          ""type"": ""string"",
          ""description"": ""Password (8+ chars, must contain uppercase, lowercase, number, special char)"",
          ""minLength"": 8,
          ""pattern"": ""^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]"",
          ""example"": ""SecurePass123!""
        },
        ""twoFactorEnabled"": {
          ""type"": ""boolean"",
          ""description"": ""Enable two-factor authentication (recommended for security)"",
          ""default"": true
        },
        ""securityQuestions"": {
          ""type"": ""array"",
          ""description"": ""Security questions for account recovery (exactly 3 required)"",
          ""minItems"": 3,
          ""maxItems"": 3,
          ""items"": {
            ""type"": ""object"",
            ""properties"": {
              ""question"": {
                ""type"": ""string"",
                ""description"": ""Security question text""
              },
              ""answer"": {
                ""type"": ""string"",
                ""description"": ""Answer to security question""
              }
            },
            ""required"": [""question"", ""answer""]
          }
        }
      },
      ""required"": [""password"", ""securityQuestions""]
    },
    ""preferences"": {
      ""type"": ""object"",
      ""description"": ""User preferences and settings with validation rules."",
      ""properties"": {
        ""language"": {
          ""type"": ""string"",
          ""description"": ""Preferred language"",
          ""enum"": [""English"", ""Spanish"", ""French"", ""German"", ""Italian"", ""Portuguese"", ""Japanese"", ""Korean"", ""Chinese""],
          ""default"": ""English""
        },
        ""timezone"": {
          ""type"": ""string"",
          ""description"": ""User's timezone (IANA format)"",
          ""example"": ""America/New_York""
        },
        ""notifications"": {
          ""type"": ""object"",
          ""description"": ""Notification preferences"",
          ""properties"": {
            ""email"": {
              ""type"": ""boolean"",
              ""description"": ""Receive email notifications"",
              ""default"": true
            },
            ""sms"": {
              ""type"": ""boolean"",
              ""description"": ""Receive SMS notifications"",
              ""default"": false
            },
            ""push"": {
              ""type"": ""boolean"",
              ""description"": ""Receive push notifications"",
              ""default"": true
            }
          }
        }
      },
      ""required"": [""language"", ""timezone"", ""notifications""]
    }
  },
  ""required"": [""personalInfo"", ""securitySettings"", ""preferences""]
}", @"
🚨 CRITICAL VALIDATION RULES:

1. PERSONAL INFORMATION:
   - First and last names must be 2-50 characters, letters only
   - Email must be valid format and unique in system
   - Date of birth must be valid date, user must be 13+ years old
   - All personal info fields are required

2. SECURITY SETTINGS:
   - Password must meet complexity requirements (8+ chars, uppercase, lowercase, number, special char)
   - Exactly 3 security questions required
   - Two-factor authentication recommended but optional
   - Password and security questions are required

3. PREFERENCES:
   - Language must be from supported list (English, Spanish, French, German, Italian, Portuguese, Japanese, Korean, Chinese)
   - Timezone must be valid IANA format
   - Notification settings are required but individual toggles are optional

⚠️ BUSINESS RULES:
- Email addresses must be unique across the system
- Users under 13 cannot create accounts (COPPA compliance)
- Security questions cannot be the same
- Password cannot contain user's name or email
- Timezone affects all date/time displays in the application
")]
public class AdvancedUserProfile
{
    public PersonalInfo PersonalInfo { get; set; } = default!;
    public SecuritySettings SecuritySettings { get; set; } = default!;
    public UserPreferences Preferences { get; set; } = default!;
}

public class PersonalInfo
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string DateOfBirth { get; set; } = default!;
}

public class SecuritySettings
{
    public string Password { get; set; } = default!;
    public bool TwoFactorEnabled { get; set; } = true;
    public List<SecurityQuestion> SecurityQuestions { get; set; } = new();
}

public class SecurityQuestion
{
    public string Question { get; set; } = default!;
    public string Answer { get; set; } = default!;
}

public class UserPreferences
{
    public string Language { get; set; } = "English";
    public string Timezone { get; set; } = default!;
    public NotificationSettings Notifications { get; set; } = default!;
}

public class NotificationSettings
{
    public bool Email { get; set; } = true;
    public bool Sms { get; set; } = false;
    public bool Push { get; set; } = true;
}

/// <summary>
/// Example tool parameters with complex custom schema.
/// </summary>
public class AdvancedUserManagementParams
{
    [Required]
    [Description("Advanced user profile configuration with comprehensive validation rules and security requirements. Follow all the detailed rules provided in the schema.")]
    public AdvancedUserProfile Profile { get; set; } = default!;
}

/// <summary>
/// Example tool with complex custom schema.
/// </summary>
public class AdvancedUserManagementTool : BaseTool<AdvancedUserManagementParams, object>
{
    public override string Name => "advanced_create_user";
    public override string Description => "Creates a new user profile with advanced validation and security requirements.";

    protected override async Task<object> InvokeTypedAsync(AdvancedUserManagementParams parameters, CancellationToken ct = default)
    {
        // Debug logging to see what we received
        Console.WriteLine("=== DEBUG: AdvancedUserManagementTool.InvokeTypedAsync ===");
        Console.WriteLine($"Parameters received: {System.Text.Json.JsonSerializer.Serialize(parameters, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
        Console.WriteLine($"Profile is null: {parameters.Profile == null}");
        Console.WriteLine($"PersonalInfo is null: {parameters.Profile?.PersonalInfo == null}");
        Console.WriteLine($"SecuritySettings is null: {parameters.Profile?.SecuritySettings == null}");
        Console.WriteLine($"Preferences is null: {parameters.Profile?.Preferences == null}");
        Console.WriteLine("=== END DEBUG ===");

        // Simulate advanced user creation with validation
        await Task.Delay(200, ct);

        // Add null checks to prevent the NullReferenceException
        var firstName = parameters.Profile?.PersonalInfo?.FirstName ?? "Unknown";
        var lastName = parameters.Profile?.PersonalInfo?.LastName ?? "Unknown";

        return new
        {
            success = true,
            message = $"Advanced user profile created successfully: {firstName} {lastName}",
            userId = Guid.NewGuid().ToString(),
            validation = new
            {
                personalInfoValid = parameters.Profile?.PersonalInfo != null,
                securitySettingsValid = parameters.Profile?.SecuritySettings != null,
                preferencesValid = parameters.Profile?.Preferences != null,
                emailUnique = true,
                ageCompliant = true
            },
            profile = parameters.Profile
        };
    }
}
