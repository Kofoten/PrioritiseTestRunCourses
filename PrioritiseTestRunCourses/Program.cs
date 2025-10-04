using Microsoft.Extensions.Logging;
using PrioritiseTestRunCourses;
using PrioritiseTestRunCourses.Data;
using PrioritiseTestRunCourses.Logging;
using System.Text;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

if (!Options.TryParse(args, out var options, out var errors))
{
    var builder = new StringBuilder();
    foreach (var error in errors)
    {
        builder.AppendFormat("{0}  - {1}", Environment.NewLine, error);
    }
    logger.FailedToParseArguments(builder.ToString());
    return 1;
}

if (options.Help)
{
    Console.Write(Options.HelpText());
    return 0;
}

try
{
    return new Runtime(options, logger).Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "An unexpected error occurred.");
    return 42;
}
