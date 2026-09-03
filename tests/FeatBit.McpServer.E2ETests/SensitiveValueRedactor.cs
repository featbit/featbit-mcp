namespace FeatBit.McpServer.E2ETests;

internal sealed class SensitiveValueRedactor
{
    private readonly HashSet<string> _values = new(StringComparer.Ordinal);

    public void Add(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _values.Add(value);
    }

    public string Redact(string value)
    {
        var redacted = value;
        foreach (var sensitiveValue in _values.OrderByDescending(item => item.Length))
            redacted = redacted.Replace(sensitiveValue, "<redacted>", StringComparison.Ordinal);

        return redacted;
    }
}
