# Custom Schema Override in AIAgentSharp

AIAgentSharp now supports overriding auto-generated schemas with custom schemas for complex domain-specific types. This allows you to provide detailed business rules, validation logic, and usage guidance that can't be captured by the standard schema generation.

## Overview

The custom schema override feature allows you to:

- Replace auto-generated schemas with domain-specific schemas
- Include detailed business rules and validation logic
- Provide comprehensive usage guidance and examples
- Define complex relationships and constraints
- Add domain-specific terminology and concepts

## When to Use Custom Schemas

Use custom schemas when you have:

- **Complex domain types** with specific business rules
- **Validation logic** that can't be expressed with standard attributes
- **Usage patterns** that need detailed explanation
- **Interdependent fields** with complex relationships
- **Domain-specific terminology** that needs clarification

## Basic Usage

### Simple Schema Override

```csharp
[ToolSchema(Schema = @"{
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
        ""description"": ""Individual preference setting""
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
```

### Advanced Schema with Business Rules

```csharp
[ToolSchema(
    Schema = @"{
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
        ""email"": {
          ""type"": ""string"",
          ""description"": ""Valid email address (required for account activation)"",
          ""format"": ""email"",
          ""example"": ""john.doe@example.com""
        }
      },
      ""required"": [""firstName"", ""email""]
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
        }
      },
      ""required"": [""password""]
    }
  }
}",
    AdditionalRules = @"
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

⚠️ BUSINESS RULES:
- Email addresses must be unique across the system
- Users under 13 cannot create accounts (COPPA compliance)
- Security questions cannot be the same
- Password cannot contain user's name or email
")]
public class AdvancedUserProfile
{
    public PersonalInfo PersonalInfo { get; set; } = default!;
    public SecuritySettings SecuritySettings { get; set; } = default!;
}
```

## Using Custom Schemas in Tools

### Tool Parameters with Custom Schema

```csharp
public class CreateUserParams
{
    [Required]
    [Description("Complete user profile configuration with personal information, preferences, and security settings.")]
    public UserProfile Profile { get; set; } = default!;
}

public class CreateUserTool : BaseTool<CreateUserParams, object>
{
    public override string Name => "create_user";
    public override string Description => "Creates a new user profile in the system with the provided information and preferences.";

    protected override async Task<object> InvokeTypedAsync(CreateUserParams parameters, CancellationToken ct = default)
    {
        // Your user creation logic here
        return new { success = true, userId = "..." };
    }
}
```

## Schema Attribute Properties

### Schema
The main JSON schema string that defines the structure and validation rules.

### AdditionalRules
Optional additional rules and guidance that will be appended to the schema description.

## Schema Generation Priority

The system checks for custom schemas in this order:

1. **Property-level custom schema** - `[ToolSchema]` on individual properties
2. **Type-level custom schema** - `[ToolSchema]` on the class/type
3. **Auto-generated schema** - Standard schema generation

## JSON Schema Compatibility

Custom schemas must follow the JSON Schema specification and be compatible with OpenAI function calling format. Key requirements:

- Valid JSON syntax
- Proper type definitions (`string`, `number`, `integer`, `boolean`, `object`, `array`)
- Correct property structure
- Valid enum values
- Proper required field arrays

## Best Practices

### 1. Keep Schemas Focused

```csharp
// Good - Focused on specific domain
[ToolSchema(Schema = @"{
  ""type"": ""object"",
  ""description"": ""Trading signal configuration with specific logic rules."",
  ""properties"": {
    ""condition"": {
      ""type"": ""integer"",
      ""description"": ""Logic condition type"",
      ""enum"": [0, 1, 2, 3, 4, 5, 6, 7]
    }
  }
}")]

// Avoid - Too generic
[ToolSchema(Schema = @"{
  ""type"": ""object"",
  ""description"": ""Generic object"",
  ""properties"": {
    ""data"": {
      ""type"": ""object"",
      ""description"": ""Some data""
    }
  }
}")]
```

### 2. Provide Clear Descriptions

```csharp
// Good - Clear and specific
"description": "Logic condition type: 0=AND, 1=OR, 2=SOME, 3=FLIP FLOP, 4=SIGNAL, 5=NOP, 6=IF-THEN-ELSE, 7=NOT"

// Avoid - Vague
"description": "The condition"
```

### 3. Include Examples

```csharp
// Good - Includes examples
"properties": {
  "name": {
    "type": "string",
    "description": "Strategy name",
    "example": "My Trading Strategy"
  }
}

// Avoid - No examples
"properties": {
  "name": {
    "type": "string",
    "description": "Strategy name"
  }
}
```

### 4. Use AdditionalRules for Complex Logic

```csharp
[ToolSchema(
    Schema = @"...",
    AdditionalRules = @"
🚨 CRITICAL RULES:
- Rule 1: Specific behavior
- Rule 2: Validation requirements
- Rule 3: Usage patterns

⚠️ VALIDATION:
- Field A must be present when Field B is set
- Field C cannot exceed 100
- Field D must follow pattern XYZ
")]
```

### 5. Validate Your Schema

Always test your custom schema to ensure it's valid JSON and follows the correct format:

```csharp
// Test your schema
var testSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""test"": {
      ""type"": ""string""
    }
  }
}";

// Validate JSON syntax
try
{
    var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(testSchema);
    Console.WriteLine("Schema is valid JSON");
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
}
```

## Error Handling

If a custom schema is invalid or can't be parsed, the system will:

1. Log a debug message with the error
2. Fall back to auto-generated schema
3. Continue normal operation

This ensures that tools continue to work even if there are schema issues.

## Migration from Auto-Generated Schemas

To migrate from auto-generated to custom schemas:

1. **Analyze the current schema** - Use the auto-generated schema as a starting point
2. **Identify missing information** - What business rules aren't captured?
3. **Design the custom schema** - Add detailed descriptions, examples, and rules
4. **Test thoroughly** - Ensure the schema works correctly
5. **Document the changes** - Update your documentation

## Examples

See the `examples/CustomSchemaExample.cs` file for comprehensive examples of:

- Simple schema overrides
- Complex schemas with business rules
- Property-level custom schemas
- Integration with tools

## Limitations

- Custom schemas must be valid JSON
- Schemas are static (no runtime generation)
- No support for conditional schemas based on runtime data
- Schema validation is basic (JSON syntax only)

## Troubleshooting

### Common Issues

1. **Invalid JSON**: Check your schema syntax
2. **Missing properties**: Ensure all required properties are defined
3. **Type mismatches**: Verify property types match your C# class
4. **Circular references**: Avoid infinite loops in object references

### Debugging

Enable debug logging to see schema generation details:

```csharp
// Check if custom schema is being used
var schema = SchemaGenerator.Generate<YourType>();
Console.WriteLine(JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true }));
```
