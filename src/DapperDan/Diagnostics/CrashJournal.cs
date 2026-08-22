using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CodeCrafty.DapperDan.Diagnostics;

internal enum CrashPoint
{
    ProcessMainEnter,
    SharedHooksInstalled,
    IosHooksInstalled,
    EfCompiledModelInlineInitializationEnabled,
    UIApplicationMainEnter,
    UIApplicationMainReturned,
    MauiAppCreateEnter,
    MauiAppCreateReady,
    MauiProgramEnter,
    SqliteNativeEnter,
    SqliteNativeReady,
    MauiBuilderEnter,
    MauiBuilderReady,
    MauiServicesReady,
    MauiBuildEnter,
    MauiBuildReady,
    CompiledModelEnter,
    CompiledModelReady,
    DatabaseOptionsReady,
    AppXamlEnter,
    AppXamlReady,
    PageConstructorEnter,
    PageXamlEnter,
    PageXamlReady,
    PageBindingReady,
    PageLoaded,
    PageAppearingEnter,
    PageAppearingReady,
    FirstResponsiveDispatch,
    ViewModelInitializeEnter,
    ViewModelInitializeReady,
    ViewModelInitializeHandledFailure,
    DatabaseInitializeEnter,
    DatabaseInitializeReady,
    DatabaseInstallEnter,
    DatabaseExistingValidateEnter,
    PackagedDatabaseOpenEnter,
    PackagedDatabaseOpenReady,
    PackagedDatabaseCopyReady,
    PackagedDatabaseValidateReady,
    PackagedDatabaseMoveReady,
    DatabaseValidateEnter,
    DatabaseValidateReady,
    DbContextCreateEnter,
    DbContextCreateReady,
    KeikiQueryEnter,
    KeikiQueryReady,
    GuardedFailure,
    UnhandledException,
    UnobservedTaskException,
    IosMarshalManagedException,
    IosMarshalObjectiveCException,
    RichButtonCommandException,
    ApplicationCompleted,
}

internal enum CrashSource
{
    GuardedSeam,
    AppDomainUnhandled,
    UnobservedTask,
    IosMarshalManaged,
    IosMarshalObjectiveC,
    HandledStartupFailure,
    HandledDataFailure,
    RichButtonCommand,
}

/// <summary>
/// Process-wide, dependency-free facade for the durable launch journal. Every
/// operation is best effort: diagnostics must never become an app failure.
/// </summary>
internal static class CrashJournal
{
    private static readonly object StartGate = new();
    private static DurableCrashJournal? _journal;
    private static int _sharedHooksInstalled;

    internal static void BeginLaunch(
        string privateDirectory,
        string exportDirectory)
    {
        try
        {
            lock (StartGate)
            {
                _journal ??= DurableCrashJournal.Begin(
                    privateDirectory,
                    exportDirectory,
                    CrashJournalIdentity.Current);
            }
        }
        catch
        {
            // A recorder failure must never keep the application from starting.
        }
    }

    internal static void InstallSharedHooks()
    {
        if (Interlocked.Exchange(ref _sharedHooksInstalled, 1) != 0)
        {
            return;
        }

        try
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        catch
        {
            // Hook installation is diagnostic-only.
        }
    }

    internal static void Checkpoint(CrashPoint point)
    {
        try
        {
            Volatile.Read(ref _journal)?.Checkpoint(point);
        }
        catch
        {
            // Best effort by design.
        }
    }

    internal static void RecordAppIdentity(string displayVersion, string buildNumber)
    {
        try
        {
            Volatile.Read(ref _journal)?.RecordAppIdentity(
                displayVersion,
                buildNumber);
        }
        catch
        {
            // Bundle metadata is useful but never required for startup.
        }
    }

    internal static void Capture(
        CrashSource source,
        CrashPoint point,
        Exception exception,
        bool? terminating)
    {
        try
        {
            Volatile.Read(ref _journal)?.Capture(
                source,
                point,
                exception,
                terminating);
        }
        catch
        {
            // Fatal paths cannot afford a secondary recorder exception.
        }
    }

    internal static void CaptureObjectiveC(
        CrashPoint point,
        string name,
        string? reason,
        IEnumerable<string>? callStack)
    {
        try
        {
            Volatile.Read(ref _journal)?.CaptureObjectiveC(
                point,
                name,
                reason,
                callStack);
        }
        catch
        {
            // Fatal paths cannot afford a secondary recorder exception.
        }
    }

    internal static void Complete()
    {
        try
        {
            Volatile.Read(ref _journal)?.Complete();
        }
        catch
        {
            // Completion metadata is less important than a clean shutdown.
        }
    }

    private static void OnUnhandledException(
        object sender,
        UnhandledExceptionEventArgs eventArgs)
    {
        try
        {
            var exception = eventArgs.ExceptionObject as Exception ??
                new InvalidOperationException(
                    $"Unhandled non-Exception object: {eventArgs.ExceptionObject?.GetType().FullName ?? "unknown"}.");

            Capture(
                CrashSource.AppDomainUnhandled,
                CrashPoint.UnhandledException,
                exception,
                eventArgs.IsTerminating);
        }
        catch
        {
            Checkpoint(CrashPoint.UnhandledException);
        }
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs eventArgs)
    {
        try
        {
            Capture(
                CrashSource.UnobservedTask,
                CrashPoint.UnobservedTaskException,
                eventArgs.Exception,
                terminating: false);
        }
        catch
        {
            Checkpoint(CrashPoint.UnobservedTaskException);
        }

        // Observation must not change runtime behavior merely because logging
        // is enabled, so deliberately do not call eventArgs.SetObserved().
    }
}

internal readonly record struct CrashJournalIdentity(
    string AppVersion,
    string Runtime,
    string OperatingSystem,
    string Architecture,
    bool IsDynamicCodeSupported,
    bool IsDynamicCodeCompiled)
{
    internal static CrashJournalIdentity Current => new(
        typeof(CrashJournal).Assembly.GetName().Version?.ToString() ?? "unknown",
        RuntimeInformation.FrameworkDescription,
        RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        RuntimeFeature.IsDynamicCodeSupported,
        RuntimeFeature.IsDynamicCodeCompiled);
}
