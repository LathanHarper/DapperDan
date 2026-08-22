using Foundation;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
    {
        CrashJournal.Checkpoint(CrashPoint.MauiAppCreateEnter);

        try
        {
            var app = MauiProgram.CreateMauiApp();
            CrashJournal.Checkpoint(CrashPoint.MauiAppCreateReady);
            return app;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.MauiAppCreateEnter,
                exception,
                terminating: true);
            throw;
        }
    }
}
