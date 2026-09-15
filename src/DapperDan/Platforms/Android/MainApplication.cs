using Android.App;
using Android.Runtime;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp()
	{
		// Match the iOS canary's local launch evidence while diagnosing Android.
		// Both destinations stay inside this app's sandbox; nothing is transmitted.
		CrashJournal.BeginLaunch(
			Path.Combine(FilesDir!.AbsolutePath, "DapperDan", "CrashJournal"),
			Path.Combine(CacheDir!.AbsolutePath, "DapperDan Diagnostics"));
		CrashJournal.InstallSharedHooks();
		// Mono must not Join a worker from the compiled model's static initializer.
		// Use the same generated-model inline path already selected by iOS Program.
		AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", isEnabled: true);
		CrashJournal.Checkpoint(CrashPoint.EfCompiledModelInlineInitializationEnabled);
		return MauiProgram.CreateMauiApp();
	}
}
