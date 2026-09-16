// These are exact diagnostic names, not namespace-prefix permissions.
const exceptionTypes = [
  'System.AggregateException', 'System.ArgumentException', 'System.ArgumentNullException',
  'System.ArgumentOutOfRangeException', 'System.BadImageFormatException',
  'System.DllNotFoundException', 'System.EntryPointNotFoundException',
  'System.ExecutionEngineException', 'System.IndexOutOfRangeException',
  'System.InvalidCastException', 'System.InvalidOperationException', 'System.InvalidProgramException',
  'System.MemberAccessException', 'System.MethodAccessException', 'System.MissingFieldException',
  'System.MissingMemberException', 'System.MissingMethodException', 'System.NotImplementedException',
  'System.NotSupportedException', 'System.NullReferenceException', 'System.ObjectDisposedException',
  'System.OperationCanceledException', 'System.OutOfMemoryException', 'System.StackOverflowException',
  'System.TimeoutException', 'System.TypeInitializationException', 'System.TypeLoadException',
  'System.UnauthorizedAccessException', 'System.IO.DirectoryNotFoundException',
  'System.IO.FileLoadException', 'System.IO.FileNotFoundException', 'System.IO.IOException',
  'System.Reflection.ReflectionTypeLoadException', 'System.Reflection.TargetInvocationException',
  'System.Resources.MissingManifestResourceException', 'System.Text.Json.JsonException',
  'Microsoft.Maui.Controls.Xaml.XamlParseException', 'Prism.Ioc.ContainerResolutionException',
  'Prism.Navigation.NavigationException', 'DryIoc.ContainerException',
  'Foundation.MonoTouchException', 'ObjCRuntime.RuntimeException',
  'NSGenericException', 'NSInternalInconsistencyException', 'NSInvalidArgumentException',
  'NSMallocException', 'NSRangeException', 'NSUnknownKeyException',
];

// Explicit public startup seams also work when a managed frame has no source line.
const publicMethods = [
  'CodeCrafty.DapperDan.App..ctor', 'CodeCrafty.DapperDan.App.InitializeComponent',
  'CodeCrafty.DapperDan.MauiProgram.CreateMauiApp',
  'Flexler.Views.MainPage..ctor', 'Flexler.Views.MainPage.InitializeComponent',
  'Flexler.Views.MainPage.OnAppearing', 'Flexler.Views.MainPage.OnLoaded',
  'Flexler.ViewModels.MainPageViewModel..ctor',
  'Flexler.Controls.MineralAtmosphereView..ctor',
  'Flexler.Controls.MineralAtmosphereView.InitializeComponent',
  'Flexler.Controls.NativePrimaryTapViewHandler.CreatePlatformView',
  'Flexler.PanelBossKit.Views.PanelBossBody_DefaultView..ctor',
  'PrimoMaterial.CrossFade_RadoDots..ctor',
];

const namePattern = /^[A-Za-z_][A-Za-z0-9_.+`]{0,239}$/;
const keyPattern = /^[A-Za-z_][A-Za-z0-9_.-]{0,159}$/;
const filePattern = /^src\/DapperDan\/(?:[A-Za-z0-9_.-]+\/)*[A-Za-z0-9_.-]+\.(?:cs|xaml)$/;
const escapeRegex = value => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const text = (value, limit) => typeof value === 'string' ? value.slice(0, limit) : '';

function trustedNames(values, pattern, limit) {
  if (!Array.isArray(values) || values.length > limit
      || values.some(value => typeof value !== 'string' || !pattern.test(value)))
    throw new Error('Invalid public diagnostic allowlist.');
  return [...new Set(values)];
}

function trustedFiles(values) {
  if (!Array.isArray(values) || values.length > 4096)
    throw new Error('Invalid public source catalog.');
  const files = new Map();
  for (const entry of values) {
    if (!entry || typeof entry.path !== 'string' || entry.path.length > 300
        || !filePattern.test(entry.path)
        || entry.path.split('/').some(part => ['.', '..', 'bin', 'obj'].includes(part))
        || !Number.isSafeInteger(entry.lineCount) || entry.lineCount < 1 || entry.lineCount > 100000
        || files.has(entry.path))
      throw new Error('Invalid public source catalog.');
    files.set(entry.path, entry.lineCount);
  }
  return files;
}

/**
 * Pure sanitizer for DurableCrashJournal's existing exception/emergency-exception
 * records. Supply catalogs from the trusted public checkout, never from a journal:
 * resourceKeys from Features/Flexler/Resources/*.xaml; sourceFiles as repository-
 * relative .cs/.xaml paths and actual line counts. Optional names extend exact
 * allowlists. The returned function performs no I/O and never returns raw text.
 *
 * `inner` is the journal's bounded, newline-delimited "Type: message" summary,
 * not a structured exception tree. Matches are mentions, not an inferred chain.
 * Source locations belong only to the outer stack. Missing matches do not prove
 * absence of a resource/type failure, and matching a key does not prove its cause.
 */
export function createExceptionSanitizer(options = {}) {
  const types = new Set([...exceptionTypes, ...trustedNames(options.exceptionTypes ?? [], namePattern, 1024)]);
  const keys = trustedNames(options.resourceKeys ?? [], keyPattern, 2048).map(key => ({
    key, pattern: new RegExp(`(?:^|[^A-Za-z0-9_.-])${escapeRegex(key)}(?=$|[^A-Za-z0-9_.-])`),
  }));
  const methods = [...new Set([...publicMethods, ...trustedNames(options.publicMethods ?? [], namePattern, 1024)])]
    .map(method => ({ method, pattern: new RegExp(`^\\s*at\\s+${escapeRegex(method)}\\s*\\(`) }));
  const files = trustedFiles(options.sourceFiles ?? []);

  return record => {
    if (!record || !['exception', 'emergency-exception'].includes(record.kind)) return null;
    const message = text(record.message, 2048);
    const stack = text(record.stack, 8192);
    // Emergency records do not contain an inner summary in the current schema.
    const inner = record.kind === 'exception' ? text(record.inner, 2048) : '';
    const safe = {
      exceptionTypeRecognized: typeof record.exceptionType === 'string' && types.has(record.exceptionType),
      innerExceptionTypeMentions: [], resourceKeyMatches: [], publicMethods: [],
      publicSourceLocations: [], stackPresent: stack.length > 0,
    };
    if (safe.exceptionTypeRecognized) safe.exceptionType = record.exceptionType;
    if (record.kind === 'exception' && Number.isInteger(record.hresult)
        && record.hresult >= -2147483648 && record.hresult <= 2147483647) safe.hresult = record.hresult;
    if (['message', 'stack', ...(record.kind === 'exception' ? ['inner'] : [])].some(field =>
      typeof record[field] === 'string' && record[field].length > (field === 'stack' ? 8192 : 2048)))
      safe.textLimitReached = true;

    for (const line of inner.split(/\r?\n/)) {
      const candidate = line.match(/^([A-Za-z_][A-Za-z0-9_.+`]{0,239}):/u)?.[1];
      if (types.has(candidate) && !safe.innerExceptionTypeMentions.includes(candidate))
        safe.innerExceptionTypeMentions.push(candidate);
      if (safe.innerExceptionTypeMentions.length === 3) break;
    }
    const messages = message + '\n' + inner;
    for (const { key, pattern } of keys) {
      if (pattern.test(messages)) safe.resourceKeyMatches.push(key);
      if (safe.resourceKeyMatches.length === 16) break;
    }

    for (const frame of stack.split(/\r?\n/).slice(0, 128)) {
      for (const { method, pattern } of methods) {
        if (safe.publicMethods.length < 12 && pattern.test(frame) && !safe.publicMethods.includes(method))
          safe.publicMethods.push(method);
      }
      if (safe.publicSourceLocations.length === 12 || !/^\s*at\s/.test(frame)) continue;
      // Both .NET's ':line N' and Mono's ':N' source-location formats are used.
      const location = frame.replaceAll('\\', '/').match(/\s+in\s+(.+?):(?:line\s+)?(\d{1,7})\s*$/u);
      if (!location || location[1].split('/').some(part => ['.', '..'].includes(part))) continue;
      for (const [file, lineCount] of files) {
        if (location[1] !== file && !location[1].endsWith('/' + file)) continue;
        const line = Number(location[2]);
        if (line >= 1 && line <= lineCount
            && !safe.publicSourceLocations.some(value => value.file === file && value.line === line))
          safe.publicSourceLocations.push({ file, line });
        break;
      }
    }
    return safe;
  };
}
