using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

/// <summary>
/// Sample MCP tools for demonstration purposes.
/// These tools can be invoked by MCP clients to perform various operations.
/// </summary>
[McpServerToolType]
public class RandomNumberTools(ILogger<RandomNumberTools> logger)
{
    [McpServerTool]
    [Description("Generates a random number between the specified minimum and maximum values.")]
    public int GetRandomNumber(
        [Description("Minimum value (inclusive)")] int min = 0,
        [Description("Maximum value (exclusive)")] int max = 100)
    {
        logger.LogInformation("MCP Tool Called: GetRandomNumber with min={Min}, max={Max}", min, max);
        var result = Random.Shared.Next(min, max);
        logger.LogInformation("MCP Tool Result: GetRandomNumber returned {Result}", result);
        return result;
    }
}
