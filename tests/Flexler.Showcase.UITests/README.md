# Flexler showcase Android UI tests

This separate Appium project exercises Dapper Dan's Flexler showcase through native Android controls. It opens the host's Witness page, enters Flexler, and follows the same route after restarting the app. The scenarios cover editing, all container options, recipe preview and clipboard output, favorite persistence and removal, responsive controls, and returning to the host and reopening in both orientations.

Build this project without an emulator using `dotnet build tests/Flexler.Showcase.UITests/Flexler.Showcase.UITests.csproj -c Release --nologo`. Building or discovering tests does not prove native functionality.

## Local verification contract

Use the existing Visual Studio-created emulator and the same Android SDK, JDK, and adb that Visual Studio uses. Resolve and record those paths, the AVD name, explicit serial, installed package hash, and test evidence before running. The package is `net.codecrafty.dapperdan`; install the exact candidate separately. For fast-deployed Debug builds, identify the APK and deployed managed assemblies together. That result is distinct from packaged Release validation.

Set these environment variables for the already prepared device and Appium server:

| Variable | Value |
| --- | --- |
| `DAPPER_FLEXLER_UI_ANDROID_SERIAL` | Explicit serial of the agreed emulator |
| `DAPPER_FLEXLER_UI_ANDROID_ACTIVITY` | Launch activity resolved from the installed Dapper Dan package |
| `DAPPER_FLEXLER_UI_APPIUM_URL` | Existing Appium server URL; defaults to `http://127.0.0.1:4725` |
| `DAPPER_FLEXLER_UI_EVIDENCE` | New, ignored local evidence directory for this run |

Then run `dotnet test tests/Flexler.Showcase.UITests/Flexler.Showcase.UITests.csproj -c Release --nologo --logger trx`. Use the repository's pinned SDK; record any explicitly agreed local SDK exception alongside the result. The project uses the centrally managed test packages and a local Appium.WebDriver 8.1.0 override.

The suite keeps app data (`noReset=true`) and removes only the uniquely named favorites it creates. It does not create an emulator, install an app, start Appium, change SDKs, or restart adb. Screenshots and native UI trees can include existing user content; keep evidence outside published source.

On emulator, adb, or Appium connectivity failure, stop the run and preserve the first error. The suite latches recognized connectivity failures and writes `NEEDS-CRAFTY.txt`; remaining scenarios cannot start another session. Load **Hollar out loud** and call for human help with the exact failure. Resume only after the agreed environment is repaired, without replacement devices or reconnect loops. Healthy-connectivity assertion failures remain ordinary product debugging.
