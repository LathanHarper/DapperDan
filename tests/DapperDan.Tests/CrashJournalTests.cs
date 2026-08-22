using System.Runtime.CompilerServices;
using System.Text.Json;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan.Tests;

public sealed class CrashJournalTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(
        Path.GetTempPath(),
        "DapperDanCrashJournalTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void CheckpointsAreImmediatelyReadableJsonLines()
    {
        var journal = BeginJournal();

        journal.Checkpoint(CrashPoint.ProcessMainEnter);
        journal.RecordAppIdentity("1.0", "11");
        journal.Checkpoint(CrashPoint.CompiledModelEnter);

        var lines = File.ReadAllLines(journal.SessionPath);
        Assert.Equal(4, lines.Length);
        Assert.All(lines, line => JsonDocument.Parse(line).Dispose());

        using var launch = JsonDocument.Parse(lines[0]);
        Assert.True(launch.RootElement.GetProperty("isDynamicCodeSupported").GetBoolean());
        Assert.False(launch.RootElement.GetProperty("isDynamicCodeCompiled").GetBoolean());

        using var last = JsonDocument.Parse(lines[^1]);
        Assert.Equal(
            nameof(CrashPoint.CompiledModelEnter),
            last.RootElement.GetProperty("point").GetString());
        Assert.Equal(4, last.RootElement.GetProperty("seq").GetInt64());

        using var identity = JsonDocument.Parse(lines[2]);
        Assert.Equal("1.0", identity.RootElement.GetProperty("displayVersion").GetString());
        Assert.Equal("11", identity.RootElement.GetProperty("buildNumber").GetString());
    }

    [Fact]
    public void CurrentIdentityCapturesRuntimeDynamicCodeCapabilities()
    {
        var identity = CrashJournalIdentity.Current;

        Assert.Equal(RuntimeFeature.IsDynamicCodeSupported, identity.IsDynamicCodeSupported);
        Assert.Equal(RuntimeFeature.IsDynamicCodeCompiled, identity.IsDynamicCodeCompiled);
    }

    [Fact]
    public void NextLaunchSealsAndExportsPreviousLaunchBeforeCreatingItsOwnFile()
    {
        var first = BeginJournal(
            new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero),
            Guid.Parse("11111111-1111-1111-1111-111111111111"));
        first.Checkpoint(CrashPoint.MauiBuildEnter);
        var firstActivePath = first.SessionPath;

        var second = BeginJournal(
            new DateTimeOffset(2026, 8, 21, 12, 1, 0, TimeSpan.Zero),
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.False(File.Exists(firstActivePath));
        var privateInterrupted = Assert.Single(Directory.EnumerateFiles(
            PrivateDirectory,
            "*.interrupted.jsonl"));
        var exportedInterrupted = Assert.Single(Directory.EnumerateFiles(
            ExportDirectory,
            "*.interrupted.jsonl"));
        Assert.Equal(
            File.ReadAllBytes(privateInterrupted),
            File.ReadAllBytes(exportedInterrupted));
        Assert.EndsWith(".active.jsonl", second.SessionPath, StringComparison.Ordinal);
        Assert.True(File.Exists(second.SessionPath));
    }

    [Fact]
    public void PartialTailIsPreservedWithoutHidingEarlierCheckpoints()
    {
        var first = BeginJournal();
        first.Checkpoint(CrashPoint.AppXamlEnter);
        File.AppendAllText(first.SessionPath, "{\"partial\":");

        _ = BeginJournal();

        var export = Assert.Single(Directory.EnumerateFiles(
            ExportDirectory,
            "*.interrupted.jsonl"));
        var exportedText = File.ReadAllText(export);
        Assert.Contains("\"point\":\"AppXamlEnter\"", exportedText, StringComparison.Ordinal);
        Assert.EndsWith("{\"partial\":", exportedText, StringComparison.Ordinal);
    }

    [Fact]
    public void ExceptionRecordsAreBoundedAndRedactCommonSecrets()
    {
        var journal = BeginJournal();
        var privateValue = Path.Combine(PrivateDirectory, "secret.db3");
        var exception = new InvalidOperationException(
            $"Failed at {privateValue} for kai@example.com via https://lan.test/report " +
            "using Bearer abc.def.ghi " + new string('x', 4_000),
            new ArgumentException("inner-value"));

        journal.Capture(
            CrashSource.HandledStartupFailure,
            CrashPoint.ViewModelInitializeHandledFailure,
            exception,
            terminating: false);

        var line = File.ReadLines(journal.SessionPath).Last();
        Assert.DoesNotContain(privateValue, line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kai@example.com", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://lan.test", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc.def.ghi", line, StringComparison.Ordinal);
        Assert.Contains("<private-path>", line, StringComparison.Ordinal);
        Assert.Contains("<email>", line, StringComparison.Ordinal);
        Assert.Contains("<url>", line, StringComparison.Ordinal);
        Assert.Contains("Bearer <redacted>", line, StringComparison.Ordinal);
        Assert.True(line.Length < 16_384);
        JsonDocument.Parse(line).Dispose();
    }

    [Fact]
    public void CompletedLaunchIsExportedWithoutBeingCalledInterrupted()
    {
        var first = BeginJournal();
        first.Complete();

        Assert.EndsWith(".completed.jsonl", first.SessionPath, StringComparison.Ordinal);
        _ = BeginJournal();

        Assert.Empty(Directory.EnumerateFiles(ExportDirectory, "*.interrupted.jsonl"));
        Assert.Single(Directory.EnumerateFiles(ExportDirectory, "*.completed.jsonl"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private string PrivateDirectory => Path.Combine(_testRoot, "private");

    private string ExportDirectory => Path.Combine(_testRoot, "documents");

    private DurableCrashJournal BeginJournal(
        DateTimeOffset? startedAt = null,
        Guid? launchId = null)
        => DurableCrashJournal.Begin(
            PrivateDirectory,
            ExportDirectory,
            new CrashJournalIdentity(
                "1.0.11",
                ".NET 10",
                "iOS",
                "Arm64",
                IsDynamicCodeSupported: true,
                IsDynamicCodeCompiled: false),
            startedAt,
            launchId);
}
