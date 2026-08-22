using Foundation;
using ObjCRuntime;
using UIKit;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan;

public class Program
{
    // This is the earliest managed entry point available to the application.
    static void Main(string[] args)
    {
        TryStartDiagnostics();

        try
        {
            CrashJournal.Checkpoint(CrashPoint.UIApplicationMainEnter);
            UIApplication.Main(args, null, typeof(AppDelegate));
            CrashJournal.Checkpoint(CrashPoint.UIApplicationMainReturned);
            CrashJournal.Complete();
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.GuardedFailure,
                exception,
                terminating: true);
            throw;
        }
    }

    private static void TryStartDiagnostics()
    {
        try
        {
            var applicationSupportUrl = NSFileManager.DefaultManager
                .GetUrls(
                    NSSearchPathDirectory.ApplicationSupportDirectory,
                    NSSearchPathDomain.User)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(applicationSupportUrl?.Path))
            {
                return;
            }

            var privateDirectory = Path.Combine(
                applicationSupportUrl.Path,
                "DapperDan",
                "CrashJournal");
            var exportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DapperDan Diagnostics");

            CrashJournal.BeginLaunch(privateDirectory, exportDirectory);
            CrashJournal.Checkpoint(CrashPoint.ProcessMainEnter);
            CrashJournal.InstallSharedHooks();
            CrashJournal.Checkpoint(CrashPoint.SharedHooksInstalled);
            IosCrashJournalHooks.Install();
            CrashJournal.Checkpoint(CrashPoint.IosHooksInstalled);
            IosCrashJournalHooks.RecordBundleIdentity();
        }
        catch
        {
            // The application must still reach UIKit when diagnostics fail.
        }
    }
}
