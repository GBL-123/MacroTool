param(
    [double]$Threshold = 75
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "== build =="
dotnet build (Join-Path $root "MacroTool.slnx") --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$testsExe = Join-Path $root "MacroTool.Tests\bin\Debug\net10.0\MacroTool.Tests.exe"
if (-not (Test-Path $testsExe)) { throw "test exe not found: $testsExe" }

$outDir = Join-Path $root "coverage"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

$coverageFile = Join-Path $outDir "coverage.cobertura.xml"

Write-Host "== run tests with coverage =="
& $testsExe --coverage --coverage-output-format cobertura --coverage-output $coverageFile
if ($LASTEXITCODE -ne 0) { throw "tests failed" }

# The MTP code coverage extension may normalize the output location; find the file if needed.
if (-not (Test-Path $coverageFile)) {
    $candidate = Get-ChildItem (Join-Path $root "MacroTool.Tests\bin\Debug\net10.0\TestResults") -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $candidate) { throw "coverage output not found" }
    $coverageFile = $candidate.FullName
}

$rgDll = Join-Path $env:USERPROFILE ".nuget\packages\reportgenerator\5.5.11\tools\net10.0\ReportGenerator.dll"

Write-Host "== generate report =="
& dotnet $rgDll "-reports:$coverageFile" "-targetdir:$(Join-Path $outDir 'report')" "-reporttypes:Html;TextSummary"
if ($LASTEXITCODE -ne 0) { throw "report generation failed" }

# Product assemblies only; test assemblies are excluded from the gate.
$productAssemblies = @(
    "MacroTool.Domain",
    "MacroTool.Application",
    "MacroTool.Infrastructure",
    "MacroTool"
)

[xml]$coverage = Get-Content -LiteralPath $coverageFile -Raw
$rows = @()
$coveredTotal = 0
$lineTotal = 0

foreach ($name in $productAssemblies) {
    $package = @($coverage.coverage.packages.package) | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if (-not $package) { throw "assembly '$name' not found in coverage report: $coverageFile" }

    $lines = @($package.SelectNodes("classes/class/lines/line"))
    $covered = 0
    foreach ($line in $lines) {
        if ([int]$line.hits -gt 0) { $covered++ }
    }
    $linesCount = $lines.Count
    $percent = if ($linesCount -gt 0) { [math]::Round(100.0 * $covered / $linesCount, 1) } else { 0.0 }

    $rows += [pscustomobject]@{ Assembly = $name; Covered = $covered; Lines = $linesCount; Percent = $percent }
    $coveredTotal += $covered
    $lineTotal += $linesCount
}

if ($lineTotal -eq 0) { throw "no coverable lines found for product assemblies" }
$percentTotal = [math]::Round(100.0 * $coveredTotal / $lineTotal, 1)

Write-Host "== product assembly line coverage =="
$rows | Format-Table -AutoSize | Out-String | Write-Host
Write-Host ("== aggregate: {0} % ({1}/{2} lines, threshold {3} %) ==" -f $percentTotal, $coveredTotal, $lineTotal, $Threshold)

if ($percentTotal -lt $Threshold) {
    Write-Host "FAIL: coverage below threshold. Report: $(Join-Path $outDir 'report\index.html')"
    exit 1
}

Write-Host ("PASS. Report: {0}" -f (Join-Path $outDir "report\index.html"))
