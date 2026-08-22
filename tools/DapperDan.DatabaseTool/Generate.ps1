param(
    [string] $DotNetPath = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$stableModelId = '4d9c20ea-da25-4127-bf04-b92a9cd5ad8a'
$modelIdPattern = 'modelId:\s*new Guid\("(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})"\)'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $PSScriptRoot 'DapperDan.DatabaseTool.csproj'
$modelOutput = Join-Path $repoRoot 'src\DapperDan\Data\CompiledModels'
$seedOutput = Join-Path $repoRoot 'src\DapperDan\Resources\Raw\dapper-dan-seed-v1.db3'
$dataRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'src\DapperDan\Data'))
$modelOutputFull = [System.IO.Path]::GetFullPath($modelOutput)
$modelParentFull = [System.IO.Path]::GetFullPath(
    ([System.IO.Path]::GetDirectoryName($modelOutputFull) ??
        $(throw 'The compiled-model output path has no parent directory.')))
$generationId = [System.Guid]::NewGuid().ToString('N')
$modelStaging = Join-Path $modelParentFull ".CompiledModels.generate.$generationId"
$modelBackup = Join-Path $modelParentFull ".CompiledModels.backup.$generationId"
$modelBackupHoldsLastGood = $false

if (-not $modelOutputFull.StartsWith($dataRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to replace a compiled-model path outside src/DapperDan/Data.'
}

function Assert-GeneratedSourceDirectory {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Description
    )

    $unexpectedEntries = @(
        Get-ChildItem -LiteralPath $Path -Force |
            Where-Object { $_.PSIsContainer -or $_.Extension -ne '.cs' }
    )
    if ($unexpectedEntries.Count -ne 0) {
        $unexpectedNames = $unexpectedEntries.Name -join ', '
        throw "$Description contains non-generated entries and will not be replaced: $unexpectedNames"
    }
}

function Remove-CompiledModelScratchDirectory {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $pathParent = [System.IO.Path]::GetDirectoryName($pathFull)
    $pathName = [System.IO.Path]::GetFileName($pathFull)
    $isExpectedParent = [string]::Equals(
        $pathParent,
        $modelParentFull,
        [System.StringComparison]::OrdinalIgnoreCase)
    $hasExpectedName =
        $pathName.StartsWith('.CompiledModels.generate.', [System.StringComparison]::Ordinal) -or
        $pathName.StartsWith('.CompiledModels.backup.', [System.StringComparison]::Ordinal)

    if (-not $isExpectedParent -or -not $hasExpectedName) {
        throw "Refusing to remove unexpected compiled-model scratch path: $pathFull"
    }

    if (Test-Path -LiteralPath $pathFull) {
        Remove-Item -LiteralPath $pathFull -Recurse -Force
    }
}

& $DotNetPath tool restore
if ($LASTEXITCODE -ne 0) {
    throw 'Could not restore the pinned dotnet-ef tool.'
}

& $DotNetPath restore $projectPath --locked-mode
if ($LASTEXITCODE -ne 0) {
    throw 'Could not restore the database tool.'
}

try {
    New-Item -ItemType Directory -Path $modelStaging -ErrorAction Stop | Out-Null

    & $DotNetPath ef dbcontext optimize `
        --project $projectPath `
        --startup-project $projectPath `
        --framework 'net10.0' `
        --configuration 'Release' `
        --context 'CodeCrafty.DapperDan.Data.DapperDanDbContext' `
        --output-dir $modelStaging `
        --namespace 'CodeCrafty.DapperDan.Data.CompiledModels'

    if ($LASTEXITCODE -ne 0) {
        throw 'Dapper Dan compiled-model generation failed.'
    }

    Assert-GeneratedSourceDirectory -Path $modelStaging -Description 'Generated compiled-model staging directory'

    $generatedFiles = @(Get-ChildItem -LiteralPath $modelStaging -File -Filter '*.cs')
    if ($generatedFiles.Count -eq 0) {
        throw 'Dapper Dan compiled-model generation produced no C# source files.'
    }

    foreach ($requiredFile in @('DapperDanDbContextModel.cs', 'DapperDanDbContextModelBuilder.cs')) {
        if (-not (Test-Path -LiteralPath (Join-Path $modelStaging $requiredFile) -PathType Leaf)) {
            throw "Dapper Dan compiled-model generation did not produce $requiredFile."
        }
    }

    $modelIdLocations = @()
    foreach ($generatedFile in $generatedFiles) {
        $source = [System.IO.File]::ReadAllText($generatedFile.FullName)
        foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($source, $modelIdPattern)) {
            $modelIdLocations += [pscustomobject]@{
                File = $generatedFile.FullName
                Source = $source
                Match = $match
            }
        }
    }

    if ($modelIdLocations.Count -ne 1) {
        throw "Expected exactly one EF compiled-model modelId, but found $($modelIdLocations.Count)."
    }

    $modelIdLocation = $modelIdLocations[0]
    $randomModelId = $modelIdLocation.Match.Groups['id']
    $null = [System.Guid]::ParseExact($randomModelId.Value, 'D')
    $canonicalSource = $modelIdLocation.Source.Remove($randomModelId.Index, $randomModelId.Length).
        Insert($randomModelId.Index, $stableModelId)
    [System.IO.File]::WriteAllText(
        $modelIdLocation.File,
        $canonicalSource,
        [System.Text.UTF8Encoding]::new($false))

    $canonicalMatches = [System.Text.RegularExpressions.Regex]::Matches(
        [System.IO.File]::ReadAllText($modelIdLocation.File),
        $modelIdPattern)
    if ($canonicalMatches.Count -ne 1 -or
        -not [string]::Equals(
            $canonicalMatches[0].Groups['id'].Value,
            $stableModelId,
            [System.StringComparison]::Ordinal)) {
        throw 'Could not canonicalize the EF compiled-model modelId.'
    }

    if (Test-Path -LiteralPath $modelOutputFull) {
        Assert-GeneratedSourceDirectory -Path $modelOutputFull -Description 'Existing compiled-model directory'
        Move-Item -LiteralPath $modelOutputFull -Destination $modelBackup -ErrorAction Stop
        $modelBackupHoldsLastGood = $true
    }

    try {
        Move-Item -LiteralPath $modelStaging -Destination $modelOutputFull -ErrorAction Stop
    }
    catch {
        if ($modelBackupHoldsLastGood -and
            -not (Test-Path -LiteralPath $modelOutputFull) -and
            (Test-Path -LiteralPath $modelBackup)) {
            Move-Item -LiteralPath $modelBackup -Destination $modelOutputFull -ErrorAction Stop
            $modelBackupHoldsLastGood = $false
        }

        throw
    }

    if ($modelBackupHoldsLastGood) {
        $modelBackupHoldsLastGood = $false
        Remove-CompiledModelScratchDirectory -Path $modelBackup
    }

    & $DotNetPath run `
        --project $projectPath `
        --framework 'net10.0' `
        --configuration 'Release' `
        --no-restore `
        -- `
        --seed $seedOutput

    if ($LASTEXITCODE -ne 0) {
        throw 'Dapper Dan seed generation failed.'
    }
}
finally {
    if (Test-Path -LiteralPath $modelStaging) {
        Remove-CompiledModelScratchDirectory -Path $modelStaging
    }

    if ($modelBackupHoldsLastGood -and
        -not (Test-Path -LiteralPath $modelOutputFull) -and
        (Test-Path -LiteralPath $modelBackup)) {
        Move-Item -LiteralPath $modelBackup -Destination $modelOutputFull -ErrorAction Stop
        $modelBackupHoldsLastGood = $false
    }

    if ((Test-Path -LiteralPath $modelBackup) -and
        (Test-Path -LiteralPath $modelOutputFull)) {
        Remove-CompiledModelScratchDirectory -Path $modelBackup
    }
}
