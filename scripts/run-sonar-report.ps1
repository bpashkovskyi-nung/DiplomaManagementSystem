# Local Sonar-rule analysis (SonarAnalyzer.CSharp). No upload.
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$reportPath = Join-Path $repoRoot "sonar-report.txt"

Push-Location $repoRoot
try {
    Write-Host "Restoring with Sonar analyzers..."
    dotnet restore /p:RunSonarAnalyzers=true | Out-Null

    Write-Host "Building with Sonar analyzers..."
    dotnet build -c Release --no-incremental /p:RunSonarAnalyzers=true /p:AnalysisLevel=latest 2>&1 |
        Tee-Object -FilePath $reportPath

    $sonarWarnings = Select-String -Path $reportPath -Pattern "warning S\d{4,5}:" -AllMatches
    $grouped = $sonarWarnings | ForEach-Object { $_.Line } |
        ForEach-Object {
            if ($_ -match "warning (S\d{4,5}):") { $Matches[1] }
        } |
        Group-Object |
        Sort-Object Count -Descending

    Write-Host ""
    Write-Host "=== Sonar report summary ==="
    Write-Host ("Total Sonar issues: {0}" -f $sonarWarnings.Count)
    if ($grouped.Count -gt 0) {
        Write-Host ""
        Write-Host "Top rules:"
        $grouped | Select-Object -First 15 | ForEach-Object {
            Write-Host ("  {0,-8} {1}" -f $_.Name, $_.Count)
        }
    }

    Write-Host ""
    Write-Host ("Full log: {0}" -f $reportPath)
}
finally {
    Pop-Location
}
