namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// App-neutral diagnostics seam for command exceptions caught by the native
/// tap pipeline. Hosts may assign a reporter without coupling controls to a
/// particular logging service or dependency-injection container.
/// </summary>
public static class RichButtonDiagnostics
{
    private static Func<RichButtonCommandExceptionContext, Task>? _commandExceptionReporter;

    public static Func<RichButtonCommandExceptionContext, Task>? CommandExceptionReporter
    {
        get => _commandExceptionReporter;
        set => _commandExceptionReporter = value;
    }

    internal static Task ReportCommandExceptionAsync(
        TapViewBase source,
        Exception exception)
    {
        var reporter = CommandExceptionReporter;

        if (reporter is null)
            return Task.CompletedTask;

        return reporter(new RichButtonCommandExceptionContext(
            source.GetType().Name,
            source.AutomationId,
            exception));
    }
}

public sealed record RichButtonCommandExceptionContext(
    string ControlName,
    string? AutomationId,
    Exception Exception);
