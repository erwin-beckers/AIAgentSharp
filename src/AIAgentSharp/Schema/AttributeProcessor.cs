using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AIAgentSharp.Schema;

/// <summary>
/// Handles processing of DataAnnotations and custom attributes for schema generation.
/// </summary>
public sealed class AttributeProcessor
{
    /// <summary>
    /// Processes ToolField attributes and applies them to the schema.
    /// </summary>
    public void ProcessToolFieldAttributes(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var toolFieldAttr = property.GetCustomAttribute<ToolFieldAttribute>();

        if (toolFieldAttr != null)
        {
            if (!string.IsNullOrEmpty(toolFieldAttr.Description))
            {
                schemaDict["description"] = toolFieldAttr.Description;
            }

            if (toolFieldAttr.Example != null)
            {
                schemaDict["example"] = toolFieldAttr.Example;
            }

            if (toolFieldAttr.MinLength >= 0)
            {
                schemaDict["minLength"] = toolFieldAttr.MinLength;
            }

            if (toolFieldAttr.MaxLength >= 0)
            {
                schemaDict["maxLength"] = toolFieldAttr.MaxLength;
            }

            if (!double.IsNaN(toolFieldAttr.Minimum))
            {
                schemaDict["minimum"] = toolFieldAttr.Minimum;
            }

            if (!double.IsNaN(toolFieldAttr.Maximum))
            {
                schemaDict["maximum"] = toolFieldAttr.Maximum;
            }

            if (!string.IsNullOrEmpty(toolFieldAttr.Pattern))
            {
                schemaDict["pattern"] = toolFieldAttr.Pattern;
            }

            if (!string.IsNullOrEmpty(toolFieldAttr.Format))
            {
                schemaDict["format"] = toolFieldAttr.Format;
            }
        }
    }

    /// <summary>
    /// Processes DataAnnotations attributes and applies them to the schema.
    /// </summary>
    public void ProcessDataAnnotationAttributes(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        ProcessRequiredAttribute(property, schemaDict);
        ProcessStringLengthAttribute(property, schemaDict);
        ProcessMinLengthAttribute(property, schemaDict);
        ProcessMaxLengthAttribute(property, schemaDict);
        ProcessRangeAttribute(property, schemaDict);
        ProcessRegularExpressionAttribute(property, schemaDict);
        ProcessEmailAddressAttribute(property, schemaDict);
        ProcessUrlAttribute(property, schemaDict);
        ProcessPhoneAttribute(property, schemaDict);
    }

    private void ProcessRequiredAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();

        if (requiredAttr != null && !string.IsNullOrEmpty(requiredAttr.ErrorMessage))
        {
            schemaDict["description"] = requiredAttr.ErrorMessage;
        }
    }

    private void ProcessStringLengthAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();

        if (stringLengthAttr != null)
        {
            if (stringLengthAttr.MaximumLength > 0)
            {
                schemaDict["maxLength"] = stringLengthAttr.MaximumLength;
            }

            if (stringLengthAttr.MinimumLength > 0)
            {
                schemaDict["minLength"] = stringLengthAttr.MinimumLength;
            }
        }
    }

    private void ProcessMinLengthAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var minLengthAttr = property.GetCustomAttribute<MinLengthAttribute>();

        if (minLengthAttr != null)
        {
            schemaDict["minLength"] = minLengthAttr.Length;
        }
    }

    private void ProcessMaxLengthAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();

        if (maxLengthAttr != null)
        {
            schemaDict["maxLength"] = maxLengthAttr.Length;
        }
    }

    private void ProcessRangeAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr != null)
        {
            if (double.TryParse(rangeAttr.Minimum.ToString(), out var min))
            {
                schemaDict["minimum"] = min;
            }

            if (double.TryParse(rangeAttr.Maximum.ToString(), out var max))
            {
                schemaDict["maximum"] = max;
            }
        }
    }

    private void ProcessRegularExpressionAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var regexAttr = property.GetCustomAttribute<RegularExpressionAttribute>();

        if (regexAttr != null)
        {
            schemaDict["pattern"] = regexAttr.Pattern;
        }
    }

    private void ProcessEmailAddressAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var emailAttr = property.GetCustomAttribute<EmailAddressAttribute>();

        if (emailAttr != null)
        {
            schemaDict["format"] = "email";
        }
    }

    private void ProcessUrlAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var urlAttr = property.GetCustomAttribute<UrlAttribute>();

        if (urlAttr != null)
        {
            schemaDict["format"] = "uri";
        }
    }

    private void ProcessPhoneAttribute(PropertyInfo property, Dictionary<string, object> schemaDict)
    {
        var phoneAttr = property.GetCustomAttribute<PhoneAttribute>();

        if (phoneAttr != null)
        {
            schemaDict["format"] = "phone";
        }
    }
}
