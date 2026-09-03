using System.Globalization;
using System.Text.Json;

namespace FeatBit.McpServer.E2ETests;

internal static class ApiJson
{
    public static JsonElement RequireData(JsonElement root, string operation)
    {
        EnsureSuccess(root, operation);
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data)
            ? data
            : root;
    }

    public static JsonElement RequireDataObject(JsonElement root, string operation)
    {
        var data = RequireData(root, operation);
        if (data.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{operation} did not return an object in data.");

        return data;
    }

    public static IReadOnlyList<JsonElement> RequireDataArray(JsonElement root, string operation)
    {
        var data = RequireData(root, operation);
        if (data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"{operation} did not return an array in data.");

        return data.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    public static IReadOnlyList<JsonElement> RequireItems(JsonElement root, string operation)
    {
        var data = RequireDataObject(root, operation);
        if (!data.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"{operation} did not return a data.items array.");

        return items.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    public static long RequireTotalCount(JsonElement root, string operation)
    {
        var data = RequireDataObject(root, operation);
        if (!data.TryGetProperty("totalCount", out var count) || !count.TryGetInt64(out var value))
            throw new InvalidOperationException($"{operation} did not return a numeric data.totalCount.");

        return value;
    }

    public static string RequireString(JsonElement element, string propertyName, string operation)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(
                $"{operation} did not return a non-empty '{propertyName}' property.");
        }

        return property.GetString()!;
    }

    public static string RequireUuid(JsonElement element, string propertyName, string operation)
    {
        var value = RequireString(element, propertyName, operation);
        if (!Guid.TryParse(value, out _))
            throw new InvalidOperationException($"{operation} returned a non-UUID '{propertyName}'.");

        return value;
    }

    public static bool RequireBoolean(JsonElement element, string propertyName, string operation)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidOperationException(
                $"{operation} did not return a Boolean '{propertyName}' property.");
        }

        return property.GetBoolean();
    }

    public static IReadOnlyList<JsonElement> RequireArrayProperty(
        JsonElement element,
        string propertyName,
        string operation)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"{operation} did not return a '{propertyName}' array.");
        }

        return property.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    public static JsonElement RequireSingleByString(
        IEnumerable<JsonElement> elements,
        string propertyName,
        string expected,
        string operation)
    {
        var matches = elements
            .Where(element =>
                element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String &&
                string.Equals(property.GetString(), expected, StringComparison.Ordinal))
            .Select(element => element.Clone())
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"{operation} expected exactly one item with {propertyName} '{expected}', but found {matches.Length}.");
        }

        return matches[0];
    }

    public static void RequireEqual(string expected, string actual, string operation, string propertyName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{operation} returned an unexpected '{propertyName}' value.");
        }
    }

    public static void RequireEqualUuid(string expected, string actual, string operation)
    {
        if (!Guid.TryParse(expected, out var expectedId) ||
            !Guid.TryParse(actual, out var actualId) ||
            expectedId != actualId)
        {
            throw new InvalidOperationException($"{operation} returned an unexpected resource ID.");
        }
    }

    public static string? TryGetString(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(propertyName, out var property) &&
           property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    public static string RequireServerSecret(JsonElement environment, string operation)
    {
        var value = TryGetServerSecret(environment);
        return value ?? throw new InvalidOperationException(
            $"{operation} did not return a Server environment secret.");
    }

    public static string? TryGetServerSecret(JsonElement environment)
    {
        if (!environment.TryGetProperty("secrets", out var secrets) ||
            secrets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var secret in secrets.EnumerateArray())
        {
            var descriptor = string.Join(
                ' ',
                TryGetString(secret, "name"),
                TryGetString(secret, "type"));
            if (!descriptor.Contains("server", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = TryGetString(secret, "value");
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    public static DateOnly ParseDeletionDate(JsonElement featureFlag, string operation)
    {
        var description = RequireString(featureFlag, "description", operation);
        const string marker = "delete-after:";
        if (!description.StartsWith(marker, StringComparison.OrdinalIgnoreCase) ||
            !DateOnly.TryParseExact(
                description[marker.Length..].Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var value))
        {
            throw new InvalidOperationException(
                $"{operation} returned an invalid delete-after date marker.");
        }

        return value;
    }

    public static bool FeatureFlagListContains(JsonElement root, string key, string operation)
    {
        EnsureSuccess(root, operation);
        var data = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var wrapped)
            ? wrapped
            : root;

        JsonElement items;
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("items", out var pagedItems))
            items = pagedItems;
        else
            items = data;

        if (items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"{operation} did not return a feature flag array.");

        return items.EnumerateArray().Any(item =>
            item.ValueKind == JsonValueKind.String
                ? string.Equals(item.GetString(), key, StringComparison.Ordinal)
                : string.Equals(TryGetString(item, "key"), key, StringComparison.Ordinal));
    }

    public static void EnsureSuccess(JsonElement root, string operation)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return;

        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            throw new InvalidOperationException($"{operation} returned an error: {error.GetString()}");

        if (root.TryGetProperty("success", out var success) &&
            success.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            !success.GetBoolean())
        {
            throw new InvalidOperationException(
                $"{operation} returned success=false{FormatErrors(root)}.");
        }
    }

    public static void EnsureMutationSuccess(JsonElement root, string operation)
    {
        EnsureSuccess(root, operation);
        if (root.ValueKind == JsonValueKind.False)
            throw new InvalidOperationException($"{operation} returned false.");
    }

    private static string FormatErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var messages = errors.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item));
        var summary = string.Join("; ", messages);
        return string.IsNullOrEmpty(summary) ? string.Empty : $": {summary}";
    }
}
