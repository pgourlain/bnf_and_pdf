[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateRange(0, 1)]
    [double]$MinimumLineRate = 0.90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repositoryRoot "pdfsharpdslTests\pdfsharpdslTests.csproj"
$settingsFile = Join-Path $repositoryRoot ".runsettings"
$resultsDirectory = Join-Path $repositoryRoot "artifacts\coverage"

if (Test-Path $resultsDirectory) {
    Remove-Item $resultsDirectory -Recurse -Force
}

& dotnet test $testProject `
    --configuration $Configuration `
    --settings $settingsFile `
    --collect:"XPlat Code Coverage" `
    --results-directory $resultsDirectory

if ($LASTEXITCODE -ne 0) {
    throw "Coverage test run failed with exit code $LASTEXITCODE."
}

$reports = @(Get-ChildItem $resultsDirectory -Filter "coverage.cobertura.xml" -Recurse)
if ($reports.Count -ne 1) {
    throw "Expected one Cobertura report, but found $($reports.Count)."
}

[xml]$coverage = Get-Content $reports[0].FullName
$packages = @($coverage.coverage.packages.package)
$unexpectedPackages = @($packages | Where-Object { $_.name -ne "PdfSharpDslCore" })
if ($packages.Count -ne 1 -or $unexpectedPackages.Count -ne 0) {
    $packageNames = ($packages | ForEach-Object { $_.name }) -join ", "
    throw "Expected coverage only for PdfSharpDslCore, but found: $packageNames"
}

$lineRate = [double]::Parse($coverage.coverage."line-rate", [Globalization.CultureInfo]::InvariantCulture)
$branchRate = [double]::Parse($coverage.coverage."branch-rate", [Globalization.CultureInfo]::InvariantCulture)

$summary = [pscustomobject]@{
    Assembly        = $packages[0].name
    LineCoverage    = "{0:P2}" -f $lineRate
    Lines            = "$($coverage.coverage.'lines-covered')/$($coverage.coverage.'lines-valid')"
    BranchCoverage  = "{0:P2}" -f $branchRate
    Branches         = "$($coverage.coverage.'branches-covered')/$($coverage.coverage.'branches-valid')"
    Report           = $reports[0].FullName
}
$summary | Format-List

if ($lineRate -lt $MinimumLineRate) {
    throw "Line coverage $($summary.LineCoverage) is below the required $(('{0:P2}' -f $MinimumLineRate))."
}