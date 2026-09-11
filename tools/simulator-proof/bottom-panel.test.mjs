import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const read = path => readFileSync(new URL(`../../${path}`, import.meta.url), 'utf8');
const pagePath = 'src/DapperDan/Views/BottomPanelCanary/BottomPanelCanaryPage';
const xaml = read(`${pagePath}.xaml`).replace(/<!--[\s\S]*?-->/g, '');
const code = read(`${pagePath}.xaml.cs`);
const viewModel = read('src/DapperDan/ViewModels/BottomPanelCanaryViewModel.cs');
const startup = read('src/DapperDan/MauiProgram.cs');
const project = read('src/DapperDan/DapperDan.csproj');
const actions = ['Alpha', 'Bravo', 'Charlie', 'Delta', 'More'];

function attributes(tag) {
  return Object.fromEntries([...tag.matchAll(/([\w:.]+)\s*=\s*(["'])([\s\S]*?)\2/g)]
    .map(([, name, , value]) => [name, value]));
}

function actionPanel() {
  const section = xaml.match(/<panelViews:PanelBossBody_DefaultView\.BottomInputPanels\s*>([\s\S]*?)<\/panelViews:PanelBossBody_DefaultView\.BottomInputPanels>/);
  assert.ok(section, 'The action panel must belong to the BottomInputPanels lane.');
  const grid = section[1].trim().match(/^<Grid\b([^>]*)>([\s\S]*)<\/Grid>$/);
  assert.ok(grid, 'The lane must contain the page-owned action Grid.');
  return { properties: attributes(grid[1]), body: grid[2] };
}

function buttons(body) {
  return [...body.matchAll(/<controls:RichButton\b([^>]*)>([\s\S]*?)<\/controls:RichButton>/g)]
    .map(([, tag, content]) => ({ properties: attributes(tag), content }));
}

const numbers = value => value.split(',').map(part => Number(part.trim()));

test('PanelBoss is the first real page child and owns the inline action lane', () => {
  const page = xaml.replace(/<\?xml[\s\S]*?\?>/, '')
    .replace(/<ContentPage\.Resources\b[^>]*>[\s\S]*?<\/ContentPage\.Resources>/g, '');
  assert.match(page, /^\s*<ContentPage\b[^>]*>\s*<panelViews:PanelBossBody_DefaultView\b/);
  const host = attributes(page.match(/<panelViews:PanelBossBody_DefaultView\b([^>]*)>/)[1]);
  assert.equal(host.AutomationId, 'BottomPanel_Host');
  assert.equal(host.PanelBossInstance, '{Binding ActivePanelBoss}');
  assert.equal(actionPanel().properties['x:Name'], 'ActionPanel');
});

test('the fixed action strip has five direct RichButton commands and stable IDs', () => {
  const { properties, body } = actionPanel();
  assert.deepEqual(properties.ColumnDefinitions.split(',').map(value => value.trim()), Array(5).fill('*'));
  const controls = buttons(body);
  assert.equal(controls.length, 5);
  controls.forEach(({ properties: button }, index) => {
    assert.equal(button.AutomationId, `BottomPanel_${actions[index]}`);
    assert.equal(button.Command, '{Binding SelectActionCommand}');
    assert.equal(button.CommandParameter, actions[index]);
    assert.equal(button['Grid.Row'], '1');
    assert.equal(button['Grid.Column'], String(index));
  });
  const remaining = body
    .replace(/<controls:RichButton\b[^>]*>[\s\S]*?<\/controls:RichButton>/g, '')
    .replace(/<Label\b[^>]*\/>/g, '').trim();
  assert.equal(remaining, '', 'No wrapping layout may change the direct Grid children.');
  const ids = [...xaml.matchAll(/\bAutomationId\s*=\s*"([^"]+)"/g)].map(match => match[1]);
  assert.equal(new Set(ids).size, ids.length, 'Automation IDs must be unique.');
  assert.doesNotMatch(xaml, /GestureRecognizers|TapGestureRecognizer|AppTabBar/);
  assert.match(viewModel, /new\s+DelegateCommand<string>\s*\(\s*SelectAction\s*\)/);
});

test('both cases retain the 124-point panel, 52/star rows, margins and centered text stacks', () => {
  const { properties, body } = actionPanel();
  assert.equal(Number(properties.HeightRequest), 124);
  assert.deepEqual(properties.RowDefinitions.split(',').map(value => value.trim()), ['52', '*']);
  assert.equal(Number(properties.RowSpacing), 0);
  assert.equal(Number(properties.ColumnSpacing), 0);
  assert.equal(properties.SafeAreaEdges, undefined, 'Baseline must retain the Grid default.');
  assert.equal(properties.VerticalOptions, undefined, 'Keep the action Grid default Fill alignment.');
  const tongue = attributes(body.match(/<Label\b([^>]*)\/>/)[1]);
  assert.equal(tongue['x:Name'], 'Tongue');
  assert.equal(Number(tongue.HeightRequest), 52);
  for (const { properties: button, content } of buttons(body)) {
    assert.equal(button.HeightRequest, undefined);
    const border = attributes(content.match(/<Border\b([^>]*)>/)[1]);
    assert.deepEqual(numbers(border.Margin), [2, 2, 2, 16]);
    const stack = attributes(content.match(/<VerticalStackLayout\b([^>]*)>/)[1]);
    assert.equal(stack.VerticalOptions, 'Center');
    assert.equal(Number(stack.Spacing), 1);
  }
});

test('the comparison changes only ActionPanel safe-area behavior before appearing', () => {
  assert.match(code, /GetEnvironmentVariable\("DAPPERDAN_PANEL_CASE"\)\s*\?\?\s*"baseline"/);
  assert.match(code, /_case\s+is\s+not\s*\(\s*"baseline"\s+or\s+"safe-area-none"\s*\)/);
  assert.match(code, /if\s*\(\s*_case\s*==\s*"safe-area-none"\s*\)\s*(?:\{\s*)?ActionPanel\.SafeAreaEdges\s*=\s*SafeAreaEdges\.None\s*;/);
  const changes = [...code.matchAll(/\b\w+\.SafeAreaEdges\s*=(?!=)/g)];
  assert.equal(changes.length, 1);
  assert.ok(changes[0].index < code.indexOf('protected override async void OnAppearing'));
});

test('measurements observe native bounds without resizing or arranging the sample', () => {
  const sample = `${code}\n${viewModel}`;
  assert.doesNotMatch(sample, /\.(?:HeightRequest|WidthRequest|MinimumHeightRequest|MaximumHeightRequest|Height|Width|Margin|Padding|RowSpacing|ColumnSpacing|VerticalOptions|HorizontalOptions|TranslationX|TranslationY|Scale|Bounds)\s*(?:=(?!=)|\+=|-=|\*=|\/=|\+\+|--)/);
  assert.doesNotMatch(sample, /\.(?:Arrange|LayoutTo|SetValue|ClearValue)\s*\(/);
  assert.match(code, /return\s+new\s+\w+\s*\(\s*view\.X\s*,\s*view\.Y\s*,\s*view\.Width\s*,\s*view\.Height\s*\)/);
  assert.match(code, /Window\?\.SafeAreaInsets\s*\?\?\s*pageView\.SafeAreaInsets/);
  assert.match(code, /JsonSerializer\.Serialize\(measurement/);
  assert.match(code, /"bottom-panel-canary\.json"/);
  for (const action of actions) assert.match(code, new RegExp(`MeasureButton\\(\\s*${action}Button\\s*,\\s*${action}Border\\s*\\)`));
});

test('normal app startup remains the fallback and canary startup requires an explicit Debug property', () => {
  const groups = [...project.matchAll(/<PropertyGroup\b([^>]*)>([\s\S]*?)<\/PropertyGroup>/g)]
    .filter(([, , body]) => body.includes('DAPPERDAN_BOTTOM_PANEL_CANARY'));
  assert.equal(groups.length, 1);
  const condition = attributes(groups[0][1]).Condition;
  assert.match(condition, /^\s*'\$\(DapperDanBottomPanelCanary\)'\s*==\s*'true'\s+And\s+'\$\(Configuration\)'\s*==\s*'Debug'\s*$/i);
  assert.match(groups[0][2], /<DefineConstants>\s*\$\(DefineConstants\);DAPPERDAN_BOTTOM_PANEL_CANARY\s*<\/DefineConstants>/);
  assert.match(startup, /#if\s+DAPPERDAN_BOTTOM_PANEL_CANARY\s+\.CreateWindow\("NavigationPage\/BottomPanelCanaryPage"\)\)\s+#else\s+\.CreateWindow\("NavigationPage\/DapperDanPage"\)\)\s+#endif/);
  assert.match(startup, /RegisterForNavigation<BottomPanelCanaryPage,\s*BottomPanelCanaryViewModel>\s*\(\)/);
});

test('the public sample uses generic labels and no private namespaces or service endpoints', () => {
  const root = attributes(xaml.match(/<ContentPage\b([^>]*)>/)[1]);
  assert.equal(root['x:Class'], 'CodeCrafty.DapperDan.Views.BottomPanelCanary.BottomPanelCanaryPage');
  for (const [name, value] of Object.entries(root).filter(([name]) => name.startsWith('xmlns:') && name !== 'xmlns:x')) {
    assert.match(value, /^clr-namespace:CodeCrafty\.DapperDan\./, name);
  }
  const labels = [...xaml.matchAll(/\b(?:Text|Title)\s*=\s*"([^"]*)"/g)]
    .map(match => match[1]).filter(value => !value.startsWith('{Binding '));
  const allowed = new Set(['Bottom panel canary', '52-point header + remaining action row',
    'A', 'B', 'C', 'D', '•••', ...actions]);
  assert.ok(labels.every(value => allowed.has(value)), 'Keep all visible copy generic to the canary.');
  assert.match(code, /namespace\s+CodeCrafty\.DapperDan\.Views\.BottomPanelCanary\s*;/);
  assert.match(viewModel, /namespace\s+CodeCrafty\.DapperDan\.ViewModels\s*;/);
  const sample = `${xaml}\n${code}\n${viewModel}`
    .replace(/https?:\/\/schemas\.microsoft\.com\/(?:dotnet\/2021\/maui|winfx\/2009\/xaml)/g, '');
  assert.doesNotMatch(sample, /https?:\/\/|\b(?:HttpClient|WebRequest|AccessToken|ApiKey|ConnectionString)\b|[A-Z]:\\/i);
});
