using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CodeCrafty.DapperDan.Diagnostics;

/// <summary>
/// A tiny synchronous black box for startup. Active sessions stay private;
/// the next launch seals and exports them before MAUI, Prism, EF, or SQLite run.
/// </summary>
internal sealed class DurableCrashJournal
{
    private const string ActiveSuffix = ".active.jsonl";
    private const string CompletedSuffix = ".completed.jsonl";
    private const string InterruptedSuffix = ".interrupted.jsonl";
    private const int MaxMessageCharacters = 2_048;
    private const int MaxStackCharacters = 8_192;
    private const int MaxInnerCharacters = 2_048;

    private readonly object _writeGate = new();
    private readonly CrashJournalIdentity _identity;
    private readonly string _launchId;
    private readonly IReadOnlyList<string> _redactionRoots;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private string _sessionPath;
    private long _sequence;

    private DurableCrashJournal(
        string sessionPath,
        string launchId,
        CrashJournalIdentity identity,
        IReadOnlyList<string> redactionRoots)
    {
        _sessionPath = sessionPath;
        _launchId = launchId;
        _identity = identity;
        _redactionRoots = redactionRoots;
    }

    internal string SessionPath => _sessionPath;

    internal static DurableCrashJournal Begin(
        string privateDirectory,
        string exportDirectory,
        CrashJournalIdentity identity,
        DateTimeOffset? startedAt = null,
        Guid? launchId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportDirectory);

        Directory.CreateDirectory(privateDirectory);

        try
        {
            Directory.CreateDirectory(exportDirectory);
            RecoverAndExportPrevious(privateDirectory, exportDirectory);
        }
        catch
        {
            // Keep the prior private evidence and still attempt a new session.
        }

        var timestamp = (startedAt ?? DateTimeOffset.UtcNow)
            .ToUniversalTime()
            .ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var id = (launchId ?? Guid.NewGuid()).ToString("N");
        var sessionName = $"session-{timestamp}-{id}";
        var sessionPath = Path.Combine(privateDirectory, sessionName + ActiveSuffix);

        using (new FileStream(
            sessionPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 1,
            FileOptions.WriteThrough))
        {
        }

        var redactionRoots = new[]
        {
            privateDirectory,
            exportDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        }
        .Where(IsSafeRedactionRoot)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(path => path.Length)
        .ToArray();

        var journal = new DurableCrashJournal(
            sessionPath,
            sessionName,
            identity,
            redactionRoots);
        journal.AppendHeader();
        return journal;
    }

    internal void Checkpoint(CrashPoint point)
    {
        lock (_writeGate)
        {
            AppendRecordCore(
                kind: "checkpoint",
                point,
                source: null,
                terminating: null,
                exceptionType: null,
                exceptionHResult: null,
                message: null,
                stack: null,
                inner: null);
        }
    }

    internal void RecordAppIdentity(string displayVersion, string buildNumber)
    {
        lock (_writeGate)
        {
            var sequence = Interlocked.Increment(ref _sequence);
            var builder = BeginRecord(sequence, "app-identity");
            AddString(builder, "displayVersion", CrashJournalText.Clean(
                displayVersion,
                maxCharacters: 128,
                redactionRoots: _redactionRoots));
            AddString(builder, "buildNumber", CrashJournalText.Clean(
                buildNumber,
                maxCharacters: 128,
                redactionRoots: _redactionRoots));
            EndRecord(builder);
            AppendLine(_sessionPath, builder.ToString());
        }
    }

    internal void Capture(
        CrashSource source,
        CrashPoint point,
        Exception exception,
        bool? terminating)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!Monitor.TryEnter(_writeGate))
        {
            WriteEmergencyRecord(source, point, exception, terminating);
            return;
        }

        try
        {
            AppendRecordCore(
                kind: "exception",
                point,
                source,
                terminating,
                exception.GetType().FullName ?? exception.GetType().Name,
                exception.HResult,
                exception.Message,
                exception.StackTrace,
                BuildInnerSummary(exception));
        }
        finally
        {
            Monitor.Exit(_writeGate);
        }
    }

    internal void CaptureObjectiveC(
        CrashPoint point,
        string name,
        string? reason,
        IEnumerable<string>? callStack)
    {
        if (!Monitor.TryEnter(_writeGate))
        {
            WriteEmergencyTextRecord(
                CrashSource.IosMarshalObjectiveC,
                point,
                name,
                reason,
                callStack is null ? null : string.Join('\n', callStack));
            return;
        }

        try
        {
            AppendRecordCore(
                kind: "exception",
                point,
                CrashSource.IosMarshalObjectiveC,
                terminating: null,
                exceptionType: name,
                exceptionHResult: null,
                message: reason,
                stack: callStack is null ? null : string.Join('\n', callStack),
                inner: null);
        }
        finally
        {
            Monitor.Exit(_writeGate);
        }
    }

    internal void Complete()
    {
        lock (_writeGate)
        {
            AppendRecordCore(
                kind: "lifecycle",
                CrashPoint.ApplicationCompleted,
                source: null,
                terminating: false,
                exceptionType: null,
                exceptionHResult: null,
                message: null,
                stack: null,
                inner: null);

            if (!_sessionPath.EndsWith(ActiveSuffix, StringComparison.Ordinal))
            {
                return;
            }

            var completedPath = _sessionPath[..^ActiveSuffix.Length] + CompletedSuffix;
            File.Move(_sessionPath, completedPath);
            _sessionPath = completedPath;
        }
    }

    private static void RecoverAndExportPrevious(
        string privateDirectory,
        string exportDirectory)
    {
        foreach (var activePath in Directory.EnumerateFiles(
            privateDirectory,
            "*" + ActiveSuffix,
            SearchOption.TopDirectoryOnly))
        {
            var interruptedPath = activePath[..^ActiveSuffix.Length] + InterruptedSuffix;
            try
            {
                File.Move(activePath, interruptedPath);
            }
            catch
            {
                // Keep the active source intact so a later launch can retry.
            }
        }

        var exportCandidates = Directory
            .EnumerateFiles(privateDirectory, "*.jsonl", SearchOption.TopDirectoryOnly)
            .Where(path =>
                path.EndsWith(InterruptedSuffix, StringComparison.Ordinal) ||
                path.EndsWith(CompletedSuffix, StringComparison.Ordinal) ||
                path.EndsWith(".emergency.jsonl", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);

        foreach (var sourcePath in exportCandidates)
        {
            TryExport(sourcePath, exportDirectory);
        }
    }

    private static void TryExport(string sourcePath, string exportDirectory)
    {
        var destinationPath = Path.Combine(
            exportDirectory,
            Path.GetFileName(sourcePath));
        if (File.Exists(destinationPath))
        {
            return;
        }

        var temporaryPath = destinationPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            using var source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16_384,
                FileOptions.WriteThrough))
            {
                source.CopyTo(destination);
                destination.Flush(flushToDisk: true);
            }

            if (new FileInfo(temporaryPath).Length != source.Length)
            {
                throw new IOException("Crash-journal export length did not match its source.");
            }

            File.Move(temporaryPath, destinationPath);
        }
        catch
        {
            // The private source remains authoritative and will be retried.
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch
            {
                // A stale .tmp is harmless and never treated as a report.
            }
        }
    }

    private void AppendHeader()
    {
        lock (_writeGate)
        {
            var sequence = Interlocked.Increment(ref _sequence);
            var builder = BeginRecord(sequence, "launch");
            AddString(builder, "appVersion", _identity.AppVersion);
            AddString(builder, "runtime", _identity.Runtime);
            AddString(builder, "os", _identity.OperatingSystem);
            AddString(builder, "architecture", _identity.Architecture);
            EndRecord(builder);
            AppendLine(_sessionPath, builder.ToString());
        }
    }

    private void AppendRecordCore(
        string kind,
        CrashPoint point,
        CrashSource? source,
        bool? terminating,
        string? exceptionType,
        int? exceptionHResult,
        string? message,
        string? stack,
        string? inner)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var builder = BeginRecord(sequence, kind);
        AddString(builder, "point", point.ToString());

        if (source is not null)
        {
            AddString(builder, "source", source.Value.ToString());
        }

        if (terminating is not null)
        {
            AddBoolean(builder, "terminating", terminating.Value);
        }

        if (exceptionType is not null)
        {
            AddString(builder, "exceptionType", exceptionType);
        }

        if (exceptionHResult is not null)
        {
            AddNumber(builder, "hresult", exceptionHResult.Value);
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddString(builder, "message", CrashJournalText.Clean(
                message,
                MaxMessageCharacters,
                _redactionRoots));
        }

        if (!string.IsNullOrWhiteSpace(stack))
        {
            AddString(builder, "stack", CrashJournalText.Clean(
                stack,
                MaxStackCharacters,
                _redactionRoots));
        }

        if (!string.IsNullOrWhiteSpace(inner))
        {
            AddString(builder, "inner", CrashJournalText.Clean(
                inner,
                MaxInnerCharacters,
                _redactionRoots));
        }

        EndRecord(builder);
        AppendLine(_sessionPath, builder.ToString());
    }

    private StringBuilder BeginRecord(long sequence, string kind)
    {
        var builder = new StringBuilder(512);
        builder.Append('{');
        AddNumber(builder, "v", 1, first: true);
        AddString(builder, "launchId", _launchId);
        AddNumber(builder, "seq", sequence);
        AddString(
            builder,
            "utc",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        AddNumber(builder, "elapsedMs", _stopwatch.ElapsedMilliseconds);
        AddNumber(builder, "thread", Environment.CurrentManagedThreadId);
        AddString(builder, "kind", kind);
        return builder;
    }

    private static void EndRecord(StringBuilder builder)
        => builder.Append('}');

    private static void AddString(
        StringBuilder builder,
        string name,
        string value,
        bool first = false)
    {
        AddPropertyPrefix(builder, name, first);
        builder.Append('"');
        AppendJsonEscaped(builder, value);
        builder.Append('"');
    }

    private static void AddNumber(
        StringBuilder builder,
        string name,
        long value,
        bool first = false)
    {
        AddPropertyPrefix(builder, name, first);
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AddBoolean(
        StringBuilder builder,
        string name,
        bool value)
    {
        AddPropertyPrefix(builder, name, first: false);
        builder.Append(value ? "true" : "false");
    }

    private static void AddPropertyPrefix(
        StringBuilder builder,
        string name,
        bool first)
    {
        if (!first)
        {
            builder.Append(',');
        }

        builder.Append('"');
        AppendJsonEscaped(builder, name);
        builder.Append("\":");
    }

    private static void AppendJsonEscaped(StringBuilder builder, string value)
    {
        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }
    }

    private string? BuildInnerSummary(Exception exception)
    {
        var innerExceptions = exception is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions.Take(3)
            : EnumerateInnerExceptions(exception).Take(3);
        var summaries = innerExceptions
            .Select(inner =>
                $"{inner.GetType().FullName}: {CrashJournalText.Clean(inner.Message, 512, _redactionRoots)}")
            .ToArray();

        return summaries.Length == 0 ? null : string.Join('\n', summaries);
    }

    private static IEnumerable<Exception> EnumerateInnerExceptions(Exception exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            yield return inner;
        }
    }

    private static bool IsSafeRedactionRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var root = Path.GetPathRoot(path);
        return string.IsNullOrEmpty(root) ||
            !string.Equals(
                path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
    }

    private void WriteEmergencyRecord(
        CrashSource source,
        CrashPoint point,
        Exception exception,
        bool? terminating)
        => WriteEmergencyTextRecord(
            source,
            point,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            exception.StackTrace,
            terminating);

    private void WriteEmergencyTextRecord(
        CrashSource source,
        CrashPoint point,
        string exceptionType,
        string? message,
        string? stack,
        bool? terminating = null)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var emergencyPath = Path.Combine(
            Path.GetDirectoryName(_sessionPath)!,
            $"{_launchId}-{sequence:D6}-{Guid.NewGuid():N}.emergency.jsonl");
        var builder = BeginRecord(sequence, "emergency-exception");
        AddString(builder, "point", point.ToString());
        AddString(builder, "source", source.ToString());
        if (terminating is not null)
        {
            AddBoolean(builder, "terminating", terminating.Value);
        }
        AddString(builder, "exceptionType", exceptionType);

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddString(builder, "message", CrashJournalText.Clean(
                message,
                MaxMessageCharacters,
                _redactionRoots));
        }

        if (!string.IsNullOrWhiteSpace(stack))
        {
            AddString(builder, "stack", CrashJournalText.Clean(
                stack,
                MaxStackCharacters,
                _redactionRoots));
        }

        EndRecord(builder);
        AppendLine(emergencyPath, builder.ToString(), FileMode.CreateNew);
    }

    private static void AppendLine(
        string path,
        string line,
        FileMode mode = FileMode.Append)
    {
        using var stream = new FileStream(
            path,
            mode,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4_096,
            FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }
}

internal static class CrashJournalText
{
    internal static string Clean(
        string value,
        int maxCharacters,
        IReadOnlyList<string> redactionRoots)
    {
        var preRedactionLimit = checked(maxCharacters * 2);
        var cleaned = (value.Length <= preRedactionLimit
                ? value
                : value[..preRedactionLimit])
            .Replace('\0', '?');

        foreach (var root in redactionRoots)
        {
            cleaned = cleaned.Replace(root, "<private-path>", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace(
                root.Replace('\\', '/'),
                "<private-path>",
                StringComparison.OrdinalIgnoreCase);
        }

        cleaned = RedactDelimited(cleaned, "Bearer ", "Bearer <redacted>");
        cleaned = RedactDelimited(cleaned, "https://", "<url>");
        cleaned = RedactDelimited(cleaned, "http://", "<url>");
        cleaned = RedactEmails(cleaned);

        if (cleaned.Length <= maxCharacters)
        {
            return cleaned;
        }

        const string suffix = "…<truncated>";
        return cleaned[..Math.Max(0, maxCharacters - suffix.Length)] + suffix;
    }

    private static string RedactDelimited(
        string value,
        string marker,
        string replacement)
    {
        var searchStart = 0;
        while (true)
        {
            var markerIndex = value.IndexOf(
                marker,
                searchStart,
                StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return value;
            }

            var end = markerIndex + marker.Length;
            while (end < value.Length && !IsSecretDelimiter(value[end]))
            {
                end++;
            }

            value = value[..markerIndex] + replacement + value[end..];
            searchStart = markerIndex + replacement.Length;
        }
    }

    private static bool IsSecretDelimiter(char character)
        => char.IsWhiteSpace(character) ||
            character is '"' or '\'' or ')' or ']' or '}' or ',' or ';';

    private static string RedactEmails(string value)
    {
        var searchStart = 0;
        while (true)
        {
            var at = value.IndexOf('@', searchStart);
            if (at <= 0 || at >= value.Length - 1)
            {
                return value;
            }

            var start = at - 1;
            while (start > 0 && IsEmailCharacter(value[start - 1]))
            {
                start--;
            }

            var end = at + 1;
            while (end < value.Length && IsEmailCharacter(value[end]))
            {
                end++;
            }

            var candidate = value[start..end];
            if (candidate.Contains('.', StringComparison.Ordinal))
            {
                const string replacement = "<email>";
                value = value[..start] + replacement + value[end..];
                searchStart = start + replacement.Length;
            }
            else
            {
                searchStart = at + 1;
            }
        }
    }

    private static bool IsEmailCharacter(char character)
        => char.IsLetterOrDigit(character) || character is '.' or '_' or '%' or '+' or '-' or '@';
}
