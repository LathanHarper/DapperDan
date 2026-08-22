using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan;

public partial class App : Application
{
    public App()
    {
        CrashJournal.Checkpoint(CrashPoint.AppXamlEnter);

        try
        {
            InitializeComponent();
            CrashJournal.Checkpoint(CrashPoint.AppXamlReady);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.AppXamlEnter,
                exception,
                terminating: true);
            throw;
        }
    }
}
