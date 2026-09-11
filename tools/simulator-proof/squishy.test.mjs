import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const read = path => readFileSync(new URL(`../../${path}`, import.meta.url), 'utf8');
const pagePath = 'src/DapperDan/Views/SquishyCanary/SquishyCanaryPage';
const xaml = read(`${pagePath}.xaml`).replace(/<!--[\s\S]*?-->/g, '');
const code = read(`${pagePath}.xaml.cs`);
const viewModel = read('src/DapperDan/ViewModels/SquishyCanaryViewModel.cs');
const project = read('src/DapperDan/DapperDan.csproj');
const startup = read('src/DapperDan/MauiProgram.cs');
const actions = ['Alpha', 'Bravo', 'Charlie', 'Delta', 'More'];

const attributes = text => Object.fromEntries([...text.matchAll(/([\w:.]+)\s*=\s*(["'])([\s\S]*?)\2/g)]
  .map(([, name, , value]) => [name, value.replace(/\s+/g, ' ').trim()]));

// A small element scanner is enough for this checked-in XAML; no package install.
function elements(xml) {
  const root = { name: '#root', properties: {}, children: [] };
  const stack = [root];
  for (const match of xml.matchAll(/<\/?[A-Za-z_][^>]*>/g)) {
    if (match[0].startsWith('</')) {
      assert.equal(stack.pop().name, match[0].slice(2, -1).trim());
      continue;
    }
    const [, name, tag] = match[0].match(/^<([\w:.]+)([\s\S]*?)\/?\s*>$/);
    const node = { name, properties: attributes(tag), children: [] };
    stack.at(-1).children.push(node);
    if (!match[0].endsWith('/>')) stack.push(node);
  }
  assert.equal(stack.length, 1);
  return root;
}

const tree = elements(xaml);
const flatten = node => [node, ...node.children.flatMap(flatten)];
const nodes = flatten(tree);
function named(name) {
  const matching = nodes.filter(node => node.properties['x:Name'] === name);
  assert.equal(matching.length, 1, `Expected exactly one ${name}.`);
  return matching[0];
}
const rows = node => node.properties.RowDefinitions.split(',').map(value => value.trim());

test('PanelBoss is the first real child and owns independent native overlay lanes', () => {
  const page = tree.children[0];
  assert.equal(page.name, 'ContentPage');
  const visualChildren = page.children.filter(node => !node.name.startsWith('ContentPage.'));
  assert.equal(visualChildren.length, 1);
  assert.equal(visualChildren[0], named('PanelBossRoot'));
  assert.equal(visualChildren[0].name, 'panelViews:PanelBossBody_DefaultView');
  assert.equal(visualChildren[0].properties.PanelBossInstance, '{Binding ActivePanelBoss}');
  for (const [laneName, panelName] of [['TopHeaderPanels', 'HeaderPanel'], ['BottomInputPanels', 'ActionPanel']]) {
    const lane = visualChildren[0].children.find(node => node.name.endsWith(`.${laneName}`));
    assert.deepEqual(lane.children, [named(panelName)]);
  }
});

test('header and action strip get natural Auto heights with five equal-width direct actions', () => {
  for (const panelName of ['HeaderPanel', 'ActionPanel']) {
    const panel = named(panelName);
    assert.deepEqual(rows(panel), ['Auto', 'Auto']);
    assert.equal(panel.properties.HeightRequest, undefined);
    assert.equal(panel.properties.MaximumHeightRequest, undefined);
  }
  assert.match(xaml, /<x:Double\s+x:Key="MinimumActionHeight"\s*>\s*56\s*<\/x:Double>/);
  const panel = named('ActionPanel');
  assert.deepEqual(panel.properties.ColumnDefinitions.split(','), Array(5).fill('*'));
  assert.equal(panel.properties.RowSpacing, '0');
  assert.equal(panel.properties.ColumnSpacing, '0');
  const buttons = panel.children.filter(node => node.name === 'controls:RichButton');
  assert.equal(buttons.length, 5);
  assert.deepEqual(panel.children.map(node => node.name), ['Label', ...Array(5).fill('controls:RichButton')]);
  buttons.forEach((button, index) => {
    assert.equal(button.properties.AutomationId, `Squishy_${actions[index]}`);
    assert.equal(button.properties.Command, '{Binding SelectActionCommand}');
    assert.equal(button.properties.CommandParameter, actions[index]);
    assert.equal(button.properties['Grid.Row'], '1');
    assert.equal(button.properties['Grid.Column'], String(index));
    assert.equal(button.properties.MinimumHeightRequest, '{StaticResource MinimumActionHeight}');
    assert.equal(button.properties.HeightRequest, undefined);
    const inner = button.children[0].children[0];
    assert.equal(inner.name, 'Grid');
    assert.deepEqual(rows(inner), ['Auto', 'Auto']);
    for (const part of ['Glyph', 'Text']) {
      const label = named(`${actions[index]}${part}`);
      assert.equal(label.properties.FontAutoScalingEnabled, 'True');
      assert.equal(label.properties.HeightRequest, undefined);
    }
  });
  assert.equal(named('Tongue').properties.HeightRequest, undefined);
  assert.equal(named('Tongue').properties.IsVisible, '{Binding IsTongueVisible}');
});

test('independent chrome sizes flow one way into Auto followers around a star viewport', () => {
  assert.deepEqual(rows(named('BodyPanel')), ['Auto', '*', 'Auto', 'Auto']);
  assert.equal(named('BodyViewport').properties['Grid.Row'], '1');
  assert.deepEqual(rows(named('BodyViewport')), ['Auto', '*', 'Auto']);
  assert.equal(named('BodyScroll').properties['Grid.Row'], '1');
  for (const [followerName, sourceName, row] of [['HeaderClearance', 'HeaderPanel', '0'], ['ActionClearance', 'ActionPanel', '2']]) {
    const follower = named(followerName);
    assert.equal(follower.name, 'BoxView');
    assert.equal(follower.properties['Grid.Row'], row);
    assert.equal(follower.properties.HeightRequest, `{Binding Height, Source={x:Reference ${sourceName}}, Mode=OneWay}`);
    assert.equal(follower.properties.IsVisible, `{Binding IsVisible, Source={x:Reference ${sourceName}}, Mode=OneWay}`);
  }
  assert.equal(named('SystemClearance').properties.HeightRequest, '{Binding ActivePanelBoss.PlatformBottomClearance, Mode=OneWay}');
  assert.equal(named('SystemClearance').properties['Grid.Row'], '3');
  assert.doesNotMatch(xaml, /\bpb:PanelBoss\.PanelClearanceFor\s*=/);
  for (const panelName of ['HeaderPanel', 'ActionPanel']) {
    assert.ok(Object.values(named(panelName).properties).every(value => !/Body|Viewport|Clearance/.test(value)));
  }
});

test('safe-area ownership is explicit and stacks only hold indeterminate bound items', () => {
  const hostSafeArea = named('PanelBossRoot').children.find(node => node.name.endsWith('.SafeAreaEdges'));
  assert.ok(hostSafeArea);
  assert.equal(hostSafeArea.children[0].properties.Default, 'Container');
  assert.ok(hostSafeArea.children[0].children.some(node => node.properties.Platform === 'Android'
    && node.properties.Value === 'Container,Container,Container,None'));
  for (const node of nodes.filter(node => ['Grid', 'ScrollView', 'Border', 'VerticalStackLayout'].includes(node.name))) {
    assert.equal(node.properties.SafeAreaEdges, 'None', node.properties['x:Name'] ?? node.name);
  }
  const stacks = nodes.filter(node => /StackLayout$/.test(node.name));
  assert.ok(stacks.length > 0);
  for (const stack of stacks) assert.equal(stack.properties['BindableLayout.ItemsSource'], '{Binding SampleRows}');
  const ids = nodes.map(node => node.properties.AutomationId).filter(Boolean);
  assert.equal(new Set(ids).size, ids.length);
  assert.doesNotMatch(xaml, /GestureRecognizers|TapGestureRecognizer|AppTabBar/);
});

test('More owns its header while following the same content-sized action strip', () => {
  const more = named('MorePanel');
  assert.deepEqual(rows(more), ['Auto', '*', 'Auto', 'Auto']);
  assert.equal(named('MoreViewport').properties['Grid.Row'], '1');
  assert.equal(named('MoreScroll').properties['Grid.Row'], '1');
  const followers = more.children.filter(node => node.name === 'BoxView');
  assert.equal(followers.length, 2);
  assert.equal(followers[0].properties.HeightRequest, '{Binding Height, Source={x:Reference ActionPanel}, Mode=OneWay}');
  assert.equal(followers[1].properties.HeightRequest, '{Binding ActivePanelBoss.PlatformBottomClearance, Mode=OneWay}');
  assert.match(viewModel, /ContentPanels_ToggleHeaderOwningPanelByName\("SquishyMorePanel"\)/);
  assert.match(viewModel, /RestoreDefaultPanelChromeAsync\("SquishyMorePanel"\)/);
});

test('diagnostics observe geometry but never assign layout or chase orientation', () => {
  const sample = `${code}\n${viewModel}`;
  assert.doesNotMatch(sample, /\.(?:HeightRequest|WidthRequest|MinimumHeightRequest|MaximumHeightRequest|Height|Width|Margin|Padding|RowSpacing|ColumnSpacing|VerticalOptions|HorizontalOptions|TranslationX|TranslationY|Scale|Bounds)\s*(?:=(?!=)|\+=|-=|\*=|\/=|\+\+|--)/);
  assert.doesNotMatch(sample, /\.(?:Arrange|LayoutTo|SetValue|ClearValue|InvalidateMeasure)\s*\(/);
  assert.doesNotMatch(sample, /OnSizeAllocated|DeviceDisplay|DisplayOrientation|Portrait|Landscape/);
  assert.match(code, /Schema\s*=\s*1\s*,\s*Scenario\s*=\s*"squishy"/);
  assert.match(code, /"squishy-canary\.json"/);
  assert.match(code, /JsonSerializerDefaults\.Web/);
  assert.match(code, /File\.Move\(path\s*\+\s*"\.pending",\s*path,\s*overwrite:\s*true\)/);
  assert.match(code, /private sealed record LayoutFrame\(double X, double Y, double Width, double Height\)/);
  assert.match(code, /NativeInsets\s*=\s*ReadNativeInsets\(\)/);
  assert.match(code, /private sealed record Insets\(double Left, double Top, double Right, double Bottom\)/);
  for (const action of actions) assert.match(code, new RegExp(`ButtonEvidence\\(${action}Button, ${action}Border, ${action}Glyph, ${action}Text\\)`));
});

test('squishy startup is an explicit Debug opt-in and preserves the previous canary and normal app', () => {
  const groups = [...project.matchAll(/<PropertyGroup\b([^>]*)>([\s\S]*?)<\/PropertyGroup>/g)]
    .filter(([, , body]) => body.includes('DAPPERDAN_SQUISHY_CANARY'));
  assert.equal(groups.length, 1);
  assert.match(attributes(groups[0][1]).Condition, /^'\$\(DapperDanSquishyCanary\)' == 'true' And '\$\(Configuration\)' == 'Debug'$/);
  assert.match(groups[0][2], /<DefineConstants>\$\(DefineConstants\);DAPPERDAN_SQUISHY_CANARY<\/DefineConstants>/);
  assert.match(startup, /#if DAPPERDAN_SQUISHY_CANARY\s+\.CreateWindow\("NavigationPage\/SquishyCanaryPage"\)\)/);
  assert.match(startup, /#if DAPPERDAN_BOTTOM_PANEL_CANARY\s+\.CreateWindow\("NavigationPage\/BottomPanelCanaryPage"\)\)/);
  assert.match(startup, /#else\s+\.CreateWindow\("NavigationPage\/DapperDanPage"\)\)/);
  assert.match(startup, /RegisterForNavigation<SquishyCanaryPage,\s*SquishyCanaryViewModel>\(\)/);
});

test('public sample is neutral and carries no private endpoints or credential material', () => {
  const sample = `${xaml}\n${code}\n${viewModel}`
    .replace(/https?:\/\/schemas\.microsoft\.com\/(?:dotnet\/2021\/maui|winfx\/2009\/xaml)/g, '');
  assert.doesNotMatch(sample, /https?:\/\/|\b(?:HttpClient|WebRequest|AccessToken|ApiKey|ConnectionString)\b|[A-Z]:\\/i);
  assert.equal(tree.children[0].properties['x:Class'], 'CodeCrafty.DapperDan.Views.SquishyCanary.SquishyCanaryPage');
  assert.match(viewModel, /Enumerable\.Range\(1, 16\)/);
  assert.match(viewModel, /This neutral content wraps naturally/);
});
