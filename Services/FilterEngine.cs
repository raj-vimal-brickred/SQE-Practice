using SQE_Practice.Models;
using System.Text.Json;

namespace SQE_Practice.Services;

public sealed class FilterEngine
{
    public bool IsMatch(TelemetryEvent telemetryEvent, List<QueryFilter>? filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return true;
        }

        foreach (var filter in filters)
        {
            if (telemetryEvent.Properties == null || 
                !telemetryEvent.Properties.TryGetValue(filter.Property, out var rawValue))
            {
                return false;
            }

            var actualStr = ExtractStringValue(rawValue);
            var expectedStr = filter.Value ?? string.Empty;

            bool matched = filter.Operator.ToLowerInvariant() switch
            {
                "eq" => actualStr.Equals(expectedStr, StringComparison.OrdinalIgnoreCase),
                "neq" => !actualStr.Equals(expectedStr, StringComparison.OrdinalIgnoreCase),
                "contains" => actualStr.Contains(expectedStr, StringComparison.OrdinalIgnoreCase),
                "startswith" => actualStr.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase),
                _ => false
            };

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static string ExtractStringValue(object? rawValue)
    {
        if (rawValue is null) return string.Empty;

        if (rawValue is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => jsonElement.GetRawText()
            };
        }

        return rawValue.ToString() ?? string.Empty;
    }
}
