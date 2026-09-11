param(
    [Parameter(Mandatory)][string]$Serial,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,
    [string]$OutputDirectory = 'artifacts/squishy-local',
    [string]$AdbPath = "${env:ProgramFiles(x86)}\Android\android-sdk\platform-tools\adb.exe"
)
$ErrorActionPreference = 'Stop'
$package = 'net.codecrafty.dapperdan'
if (-not (Test-Path -LiteralPath $AdbPath)) { throw 'Use the existing Visual Studio Android SDK adb.' }
$state = & $AdbPath -s $Serial get-state
if ($LASTEXITCODE -ne 0 -or $state -ne 'device') { throw 'Selected device is not ready.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

& $AdbPath -s $Serial shell screencap -p /sdcard/dapperdan-squishy.png
if ($LASTEXITCODE -ne 0) { throw 'Native screenshot failed.' }
& $AdbPath -s $Serial pull /sdcard/dapperdan-squishy.png (Join-Path $OutputDirectory "$Name.png")
if ($LASTEXITCODE -ne 0) { throw 'Screenshot retrieval failed.' }

$rawTree = (& $AdbPath -s $Serial exec-out uiautomator dump /dev/tty) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'UI tree capture failed.' }
$start = $rawTree.IndexOf('<?xml')
$end = $rawTree.IndexOf('</hierarchy>')
if ($start -lt 0 -or $end -lt 0) { throw 'UI tree is incomplete.' }
$xml = $rawTree.Substring($start, $end + 12 - $start)
$xml | Out-File -LiteralPath (Join-Path $OutputDirectory "$Name.xml") -Encoding utf8

$json = (& $AdbPath -s $Serial exec-out run-as $package cat files/squishy-canary.json) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'No current squishy-canary measurement was found.' }
$measurement = $json | ConvertFrom-Json
if ($measurement.schema -ne 1 -or $measurement.scenario -ne 'squishy') { throw 'Wrong measurement schema.' }
if ([DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse($measurement.capturedUtc) -gt [TimeSpan]::FromMinutes(5)) {
    throw 'Measurement is stale; do not treat it as current proof.'
}
[xml]$tree = $xml
function Find-UiNode([string]$Id) {
    $nodes = $tree.SelectNodes("//node[@resource-id='$package`:id/$Id']")
    if ($nodes.Count -ne 1) { throw "Expected one visible UI node for $Id." }
    return $nodes[0]
}
function Read-UiBounds($Node) {
    return @([regex]::Matches($Node.bounds, '-?\d+') | ForEach-Object { [double]$_.Value })
}
$hostBounds = Read-UiBounds (Find-UiNode 'Squishy_Host')
$scaleX = ($hostBounds[2] - $hostBounds[0]) / $measurement.host.width
$scaleY = ($hostBounds[3] - $hostBounds[1]) / $measurement.host.height
if ($scaleX -le 0 -or [Math]::Abs($scaleX - $scaleY) -gt 0.01) { throw 'Host coordinate scale does not match the native UI.' }
function Assert-UiFrame([string]$Id, $Frame) {
    $bounds = Read-UiBounds (Find-UiNode $Id)
    $expected = @(
        ($hostBounds[0] + $Frame.x * $scaleX),
        ($hostBounds[1] + $Frame.y * $scaleY),
        ($hostBounds[0] + ($Frame.x + $Frame.width) * $scaleX),
        ($hostBounds[1] + ($Frame.y + $Frame.height) * $scaleY)
    )
    for ($i = 0; $i -lt 4; $i++) {
        if ([Math]::Abs($bounds[$i] - $expected[$i]) -gt 2) {
            throw "Measurement disagrees with current native UI bounds for $Id. Capture again after layout settles."
        }
    }
}
$moreInUi = $tree.SelectNodes("//node[@resource-id='$package`:id/Squishy_MoreViewport']").Count -eq 1
$tongueInUi = $tree.SelectNodes("//node[@resource-id='$package`:id/Squishy_Tongue']").Count -eq 1
if ($measurement.moreVisible -ne $moreInUi -or $measurement.tongueVisible -ne $tongueInUi) {
    throw 'Measurement panel visibility disagrees with the current native UI.'
}
$viewportId = if ($moreInUi) { 'Squishy_MoreViewport' } else { 'Squishy_Viewport' }
Assert-UiFrame $viewportId $measurement.viewport
Assert-UiFrame 'Squishy_Actions' $measurement.actions
foreach ($button in $measurement.buttons) { Assert-UiFrame $button.id $button.button }
$json | Out-File -LiteralPath (Join-Path $OutputDirectory "$Name.json") -Encoding utf8

$tree.SelectNodes('//node') | Where-Object { $_.'resource-id' -match '/Squishy_' } |
    ForEach-Object { [pscustomobject]@{ Id = $_.'resource-id'; Bounds = $_.bounds; Text = $_.text } } |
    ConvertTo-Json | Out-File -LiteralPath (Join-Path $OutputDirectory "$Name-ui-summary.json") -Encoding utf8

[pscustomobject]@{
    Evidence = (Join-Path $OutputDirectory $Name)
    TextMode = $measurement.textMode
    MoreVisible = $measurement.moreVisible
    TongueVisible = $measurement.tongueVisible
    ActionHeight = $measurement.actions.height
    ViewportHeight = $measurement.viewport.height
    HeaderGap = $measurement.viewport.y - $measurement.header.y - $measurement.header.height
    ActionGap = $measurement.actions.y - $measurement.viewport.y - $measurement.viewport.height
} | Format-List
