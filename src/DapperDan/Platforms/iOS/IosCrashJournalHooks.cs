using Foundation;
using ObjCRuntime;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan;

internal static class IosCrashJournalHooks
{
    private static int _installed;

    internal static void Install()
    {
        if (Interlocked.Exchange(ref _installed, 1) != 0)
        {
            return;
        }

        try
        {
            Runtime.MarshalManagedException += OnMarshalManagedException;
            Runtime.MarshalObjectiveCException += OnMarshalObjectiveCException;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.IosHooksInstalled,
                exception,
                terminating: false);
        }
    }

    internal static void RecordBundleIdentity()
    {
        try
        {
            var bundle = NSBundle.MainBundle;
            var displayVersion = bundle
                .ObjectForInfoDictionary("CFBundleShortVersionString")
                ?.ToString() ?? "unknown";
            var buildNumber = bundle
                .ObjectForInfoDictionary("CFBundleVersion")
                ?.ToString() ?? "unknown";
            CrashJournal.RecordAppIdentity(displayVersion, buildNumber);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.IosHooksInstalled,
                exception,
                terminating: false);
        }
    }

    private static void OnMarshalManagedException(
        object? sender,
        MarshalManagedExceptionEventArgs eventArgs)
    {
        try
        {
            CrashJournal.Capture(
                CrashSource.IosMarshalManaged,
                CrashPoint.IosMarshalManagedException,
                eventArgs.Exception,
                terminating: null);
        }
        catch
        {
            CrashJournal.Checkpoint(CrashPoint.IosMarshalManagedException);
        }
    }

    private static void OnMarshalObjectiveCException(
        object? sender,
        MarshalObjectiveCExceptionEventArgs eventArgs)
    {
        try
        {
            var exception = eventArgs.Exception;
            CrashJournal.CaptureObjectiveC(
                CrashPoint.IosMarshalObjectiveCException,
                exception.Name.ToString(),
                exception.Reason,
                exception.CallStackSymbols);
        }
        catch
        {
            CrashJournal.Checkpoint(CrashPoint.IosMarshalObjectiveCException);
        }
    }
}
