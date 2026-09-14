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

$summary = Join-Path $outDir "report\Summary.txt"
$summaryText = Get-Content $summary -Raw

# Product assembly = MacroTool (test assemblies excluded from the gate).
# TextSummary prints the assembly row as "MacroTool    58.3%".
$productLine = ($summaryText -split "`r?`n") | Where-Object { $_ -match "^\s*MacroTool\s+[\d.,]+%" } | Select-Object -First 1
if (-not $productLine) { throw "MacroTool assembly row not found in Summary.txt" }
$percentText = [regex]::Match($productLine, "(\d+[\.,]?\d*)%").Groups[1].Value
$percent = [double]($percentText -replace ",", ".")

Write-Host ("== MacroTool.dll line coverage: {0} % (threshold {1} %) ==" -f $percent, $Threshold)

if ($percent -lt $Threshold) {
    Write-Host "FAIL: coverage below threshold. Report: $(Join-Path $outDir 'report\index.html')"
    exit 1
}

Write-Host ("PASS. Report: {0}" -f (Join-Path $outDir "report\index.html"))
