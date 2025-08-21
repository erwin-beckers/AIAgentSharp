using System.ComponentModel;

namespace AIAgentSharp.Schema;

/// <summary>
/// Attribute that allows overriding the auto-generated schema with a custom schema for complex types.
/// This is useful for domain-specific types that have complex rules and behaviors that can't be
/// captured by the standard schema generation.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when you have complex types with specific business rules, validation logic,
/// or usage patterns that need to be communicated to the LLM. The custom schema can include:
/// </para>
/// <list type="bullet">
/// <item><description>Detailed field descriptions and examples</description></item>
/// <item><description>Business rules and constraints</description></item>
/// <item><description>Usage patterns and best practices</description></item>
/// <item><description>Validation requirements</description></item>
/// <item><description>Domain-specific terminology and concepts</description></item>
/// </list>
/// <para>
/// The custom schema should follow the JSON Schema specification and be compatible with
/// OpenAI function calling format.
/// </para>
/// </remarks>
/// <example>
/// <para>Basic usage with a simple custom schema:</para>
/// <code>
/// [ToolSchema(Schema = @"
/// {
///   ""type"": ""object"",
///   ""properties"": {
///     ""strategy"": {
///       ""type"": ""object"",
///       ""description"": ""Complete trading strategy configuration with entry/exit signals, ATM settings, and risk management parameters."",
///       ""properties"": {
///         ""name"": {
///           ""type"": ""string"",
///           ""description"": ""Strategy name"",
///           ""example"": ""My Trading Strategy""
///         },
///         ""signals"": {
///           ""type"": ""array"",
///           ""description"": ""Array of signal rules with specific logic conditions"",
///           ""items"": {
///             ""type"": ""object"",
///             ""description"": ""Signal rule with condition type and expressions""
///           }
///         }
///       },
///       ""required"": [""name"", ""signals""]
///     }
///   },
///   ""required"": [""strategy""]
/// }")]
/// public class BacktestParams
/// {
///     public SignalTemplate Strategy { get; set; } = default!;
/// }
/// </code>
/// <para>Advanced usage with detailed rules and guidance:</para>
/// <code>
/// [ToolSchema(Schema = @"
/// {
///   ""type"": ""object"",
///   ""description"": ""Trading strategy configuration with comprehensive signal logic and risk management."",
///   ""properties"": {
///     ""strategy"": {
///       ""type"": ""object"",
///       ""description"": ""Complete trading strategy configuration. CRITICAL: LogicRule with Condition = 4 (Signal) MUST have both Long AND Short expressions and CANNOT have SubRules."",
///       ""properties"": {
///         ""signals"": {
///           ""type"": ""array"",
///           ""description"": ""Signal rules array. Each rule must follow specific condition behavior rules."",
///           ""items"": {
///             ""type"": ""object"",
///             ""description"": ""LogicRule with specific condition behavior. AND (0) requires 2+ SubRules, OR (1) requires 2+ SubRules, SOME (2) requires 2+ SubRules + MinSignals, FLIP FLOP (3) requires exactly 2 SubRules, SIGNAL (4) requires NO SubRules + both Long/Short expressions.""
///           }
///         }
///       }
///     }
///   }
/// }")]
/// public class BacktestParams
/// {
///     public SignalTemplate Strategy { get; set; } = default!;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class ToolSchemaAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the custom JSON schema that should be used instead of the auto-generated schema.
    /// </summary>
    public string Schema { get; set; }

    /// <summary>
    /// Gets or sets additional rules and guidance that should be included with the schema.
    /// This can include business rules, validation logic, usage patterns, etc.
    /// </summary>
    public string? AdditionalRules { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolSchemaAttribute"/> class.
    /// </summary>
    /// <param name="schema">The custom JSON schema string.</param>
    /// <param name="additionalRules">Optional additional rules and guidance.</param>
    public ToolSchemaAttribute(string schema, string? additionalRules = null)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        AdditionalRules = additionalRules;
    }
}
