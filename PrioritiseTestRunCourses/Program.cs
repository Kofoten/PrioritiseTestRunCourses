using Microsoft.Extensions.Logging;
using PrioritiseTestRunCourses;
using PrioritiseTestRunCourses.Data;
using PrioritiseTestRunCourses.Extensions;
using PrioritiseTestRunCourses.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

if (!Options.TryParse(args, out var options, out var errors))
{
    logger.FailedToParseArguments(errors.FormatErrors());
    return ExitCode.FailedToParseArguments;
}

if (options.Help)
{
    Console.Write(Options.HelpText());
    return ExitCode.Success;
}

var runtime = new Runtime(options, logger);
try
{
    var result = runtime.Run();
    return result switch
    {
        Success<Unit, ErrorCode> _ => ExitCode.Success,
        Failure<Unit, ErrorCode> failure => failure.Error switch
        {
            ErrorCode.FailedToLoadFile => ExitCode.FailedToLoadFile,
            ErrorCode.NoSolutionFound => ExitCode.NoSolutionFound,
            _ => ExitCode.UnexpectedErrorCode,
        },
        _ => ExitCode.UnknownResult,
    };
}
catch (Exception ex)
{
    logger.LogCritical(ex, "An unexpected error occurred.");
    return ExitCode.UnhandledException;
}
