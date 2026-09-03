namespace FeatBit.McpServer.E2ETests;

internal static class E2EProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        E2EOptions options;
        try
        {
            options = E2EOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            E2EOptions.PrintUsage(Console.Error);
            return 1;
        }

        if (options.ShowHelp)
        {
            E2EOptions.PrintUsage(Console.Out);
            return 0;
        }

        if (options.Preflight)
            return await new E2EPreflightRunner(options).RunAsync();

        if (!options.Execute)
        {
            Console.WriteLine("No SaaS calls were made. Pass --execute to run the live E2E scenario.");
            Console.WriteLine();
            E2EOptions.PrintUsage(Console.Out);
            return 0;
        }

        if (IsContinuousIntegration())
        {
            Console.Error.WriteLine(
                "Refusing to run the live FeatBit SaaS E2E scenario because a CI environment was detected.");
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        return await new E2ERunner(options).RunAsync(cancellation.Token);
    }

    private static bool IsContinuousIntegration()
    {
        var value = Environment.GetEnvironmentVariable("CI");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }
}
