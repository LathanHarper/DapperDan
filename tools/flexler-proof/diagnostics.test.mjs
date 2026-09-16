import test from 'node:test';
import assert from 'node:assert/strict';
import { createExceptionSanitizer } from './diagnostics.mjs';

const page = 'src/DapperDan/Features/Flexler/MainPage.xaml.cs';
const viewModel = 'src/DapperDan/Features/Flexler/MainPageViewModel.cs';
const options = {
  resourceKeys: ['Flexler.VoidBrush', 'Flexler.BodyFontFamily'],
  sourceFiles: [{ path: page, lineCount: 120 }, { path: viewModel, lineCount: 500 }],
};

test('summarizes the real managed journal schema without publishing message text', () => {
  const safe = createExceptionSanitizer(options)({
    kind: 'exception', exceptionType: 'System.TypeInitializationException', hresult: -2146233036,
    message: 'Failure for person@example.invalid.',
    inner: 'Microsoft.Maui.Controls.Xaml.XamlParseException: StaticResource not found for key Flexler.VoidBrush\nSystem.MissingMethodException: constructor details',
    stack: `   at Flexler.Views.MainPage.InitializeComponent() in /checkout/${page}:line 18\n   at Flexler.Views.MainPage..ctor() in /checkout/${page}:line 12`,
  });
  assert.deepEqual(safe, {
    exceptionTypeRecognized: true,
    innerExceptionTypeMentions: ['Microsoft.Maui.Controls.Xaml.XamlParseException', 'System.MissingMethodException'],
    resourceKeyMatches: ['Flexler.VoidBrush'],
    publicMethods: ['Flexler.Views.MainPage.InitializeComponent', 'Flexler.Views.MainPage..ctor'],
    publicSourceLocations: [{ file: page, line: 18 }, { file: page, line: 12 }],
    stackPresent: true, exceptionType: 'System.TypeInitializationException', hresult: -2146233036,
  });
});

test('arbitrary identities, parameters, private paths and workflow text cannot escape', () => {
  const privateText = 'SYNTHETIC_PRIVATE_MARKER';
  const safe = createExceptionSanitizer(options)({
    kind: 'exception', exceptionType: `System.${privateText}`,
    message: `${privateText}@example.invalid https://example.invalid/${privateText} Bearer ${privateText}`,
    inner: `Private.${privateText}: ::warning::${privateText}`,
    stack: `at Flexler.Views.MainPage..ctor(${privateText}) in /Users/${privateText}/${page}:line 12`,
    launchId: privateText, source: privateText, secret: privateText, point: privateText,
  });
  assert.equal(JSON.stringify(safe).includes(privateText), false);
  assert.equal(safe.exceptionTypeRecognized, false);
  assert.equal('exceptionType' in safe, false);
  assert.deepEqual(safe.publicSourceLocations, [{ file: page, line: 12 }]);
});

test('flat inner summaries expose at most three known unique type mentions, not a tree', () => {
  const safe = createExceptionSanitizer()({ kind: 'exception', inner: [
    'System.TypeLoadException: type detail', 'System.TypeLoadException: duplicate',
    'Unlisted.SecretException: private', 'System.MissingMethodException: method detail',
    'DryIoc.ContainerException: resolution detail', 'System.NullReferenceException: fourth known mention',
  ].join('\n') });
  assert.deepEqual(safe.innerExceptionTypeMentions,
    ['System.TypeLoadException', 'System.MissingMethodException', 'DryIoc.ContainerException']);
  assert.equal('chain' in safe, false);
});

test('does not invent support for a structured inner exception payload', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'exception', inner: {
    exceptionType: 'System.MissingMethodException', message: 'Flexler.VoidBrush',
  } });
  assert.deepEqual(safe.innerExceptionTypeMentions, []);
  assert.deepEqual(safe.resourceKeyMatches, []);
});

test('Objective-C records keep known names but omit reasons, addresses and call-stack symbols', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'exception',
    exceptionType: 'NSInvalidArgumentException', message: 'private selector value',
    stack: '0 CoreFoundation 0x000012345 __exceptionPreprocess + 172\n1 PrivateApplication 0x00006789 private_symbol + 1',
  });
  assert.equal(safe.exceptionType, 'NSInvalidArgumentException');
  assert.equal(safe.stackPresent, true);
  assert.deepEqual(safe.publicMethods, []);
  assert.deepEqual(safe.publicSourceLocations, []);
  assert.doesNotMatch(JSON.stringify(safe), /private|0x000|CoreFoundation|selector/i);
});

test('emergency records do not acquire unsupported inner or hresult fields', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'emergency-exception',
    exceptionType: 'System.MissingMethodException', hresult: -1,
    message: 'Flexler.VoidBrush', inner: 'System.TypeLoadException: Flexler.BodyFontFamily',
  });
  assert.equal(safe.exceptionType, 'System.MissingMethodException');
  assert.equal('hresult' in safe, false);
  assert.deepEqual(safe.innerExceptionTypeMentions, []);
  assert.deepEqual(safe.resourceKeyMatches, ['Flexler.VoidBrush']);
});

test('resource matching is exact, case-sensitive and cannot leak surrounding text', () => {
  const sanitize = createExceptionSanitizer(options);
  assert.deepEqual(sanitize({ kind: 'exception',
    message: 'FlexlerXVoidBrush Flexler.VoidBrushExtra prefixFlexler.VoidBrush Flexler.VoidBrush.Child flexler.VoidBrush Flexler.VoidBrush-private',
  }).resourceKeyMatches, []);
  assert.deepEqual(sanitize({ kind: 'exception',
    message: 'Unknown key "Flexler.VoidBrush" (Flexler.BodyFontFamily). Secret.OtherKey',
  }).resourceKeyMatches, ['Flexler.VoidBrush', 'Flexler.BodyFontFamily']);
});

test('Windows and Mono paths map to repository-relative files and valid lines only', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'exception', stack:
    `at Flexler.Views.MainPage..ctor () [0x0000a] in C:\\work\\${page.replaceAll('/', '\\')}:12\n`
    + `at Flexler.Views.MainPage.InitializeComponent() in /build/${page}:line 18\n`
    + `at Flexler.Views.MainPage.InitializeComponent() in /build/${page}:line 18`,
  });
  assert.deepEqual(safe.publicSourceLocations, [{ file: page, line: 12 }, { file: page, line: 18 }]);
});

test('unknown files, basename collisions, traversal and impossible line numbers are excluded', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'exception', stack: [
    'at Example.Call() in /private/MainPage.xaml.cs:line 12',
    `at Example.Call() in /build/not-${page}:line 12`,
    `at Example.Call() in /build/../${page}:line 12`,
    `at Example.Call() in /build/${page}:line 0`,
    `at Example.Call() in /build/${page}:line 121`,
    `arbitrary text in /build/${page}:line 12`,
  ].join('\n') });
  assert.deepEqual(safe.publicSourceLocations, []);
});

test('method-only frames require an exact explicit public method, not just a namespace', () => {
  const safe = createExceptionSanitizer()({ kind: 'exception', stack: [
    'at Flexler.Views.MainPage.InitializeComponent()',
    'at Flexler.Views.MainPage.InitializeComponentPrivate()',
    'at Flexler.Views.MainPage.SecretMethod()',
    'at Prefix.Flexler.Views.MainPage.InitializeComponent()',
    'arbitrary text Flexler.Views.MainPage.InitializeComponent()',
  ].join('\n') });
  assert.deepEqual(safe.publicMethods, ['Flexler.Views.MainPage.InitializeComponent']);
});

test('non-exception and malformed records add no exception diagnostics', () => {
  const sanitize = createExceptionSanitizer();
  for (const record of [null, undefined, 42, [], {}, { kind: 'checkpoint' }, { kind: 'exception\n::error::text' }])
    assert.equal(sanitize(record), null);
  const safe = sanitize({ kind: 'exception', message: 42, stack: {}, inner: [], exceptionType: {}, hresult: 2 ** 40 });
  assert.equal(safe.stackPresent, false);
  assert.equal('hresult' in safe, false);
});

test('journal text limits bound inspection and report truncation rather than scanning hidden tails', () => {
  const safe = createExceptionSanitizer(options)({ kind: 'exception',
    message: 'x'.repeat(2048) + ' Flexler.VoidBrush',
    inner: 'x'.repeat(2048) + '\nSystem.TypeLoadException: private',
    stack: 'x'.repeat(8192) + `\nat Flexler.Views.MainPage.InitializeComponent() in /build/${page}:line 18`,
  });
  assert.equal(safe.textLimitReached, true);
  assert.deepEqual(safe.resourceKeyMatches, []);
  assert.deepEqual(safe.innerExceptionTypeMentions, []);
  assert.deepEqual(safe.publicMethods, []);
  assert.deepEqual(safe.publicSourceLocations, []);
});

test('resource and source-location outputs are bounded even for many recognized matches', () => {
  const keys = Array.from({ length: 30 }, (_, index) => `Flexler.Key${index}`);
  const safe = createExceptionSanitizer({ resourceKeys: keys, sourceFiles: options.sourceFiles })({
    kind: 'exception', message: keys.join(' '),
    stack: Array.from({ length: 30 }, (_, index) => `at Example.Call() in /build/${page}:line ${index + 1}`).join('\n'),
  });
  assert.equal(safe.resourceKeyMatches.length, 16);
  assert.equal(safe.publicSourceLocations.length, 12);
});

test('catalog entries are copied so later caller mutation cannot expand the allowlist', () => {
  const catalog = { resourceKeys: ['Flexler.VoidBrush'], sourceFiles: [{ path: page, lineCount: 12 }], exceptionTypes: [] };
  const sanitize = createExceptionSanitizer(catalog);
  catalog.resourceKeys.push('Secret.Key');
  catalog.sourceFiles[0].lineCount = 999;
  catalog.exceptionTypes.push('Secret.Exception');
  const safe = sanitize({ kind: 'exception', exceptionType: 'Secret.Exception', message: 'Secret.Key',
    stack: `at Example.Call() in /build/${page}:line 99` });
  assert.equal(safe.exceptionTypeRecognized, false);
  assert.deepEqual(safe.resourceKeyMatches, []);
  assert.deepEqual(safe.publicSourceLocations, []);
});

test('extra trusted public types and methods are explicit exact additions', () => {
  const safe = createExceptionSanitizer({ exceptionTypes: ['Public.DemoException'],
    publicMethods: ['CodeCrafty.DapperDan.Demo.Open'] })({ kind: 'exception',
    exceptionType: 'Public.DemoException', stack: 'at CodeCrafty.DapperDan.Demo.Open(private argument)' });
  assert.equal(safe.exceptionType, 'Public.DemoException');
  assert.deepEqual(safe.publicMethods, ['CodeCrafty.DapperDan.Demo.Open']);
});

test('invalid, external and generated-file catalogs fail closed with a constant error', () => {
  for (const catalog of [
    { resourceKeys: ['bad\n::warning::message'] }, { exceptionTypes: ['private@example.invalid'] },
    { publicMethods: ['/private/path'] }, { resourceKeys: Array(2049).fill('Key') },
    { sourceFiles: [{ path: '../private.cs', lineCount: 4 }] },
    { sourceFiles: [{ path: 'src/DapperDan/../Private.cs', lineCount: 4 }] },
    { sourceFiles: [{ path: 'src/DapperDan/obj/Generated.cs', lineCount: 4 }] },
    { sourceFiles: [{ path: page, lineCount: 0 }] },
    { sourceFiles: [{ path: page, lineCount: 1.5 }] },
    { sourceFiles: [{ path: page, lineCount: 100001 }] },
    { sourceFiles: [{ path: page, lineCount: 1 }, { path: page, lineCount: 1 }] },
  ]) assert.throws(() => createExceptionSanitizer(catalog), /^Error: Invalid public (diagnostic allowlist|source catalog)\.$/);
});
