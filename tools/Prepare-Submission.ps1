param(
    [string]$OutputRoot = "submission-output"
)

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$targetRoot = Join-Path $workspaceRoot $OutputRoot

if (Test-Path $targetRoot) {
    Remove-Item $targetRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null

$clientTarget = Join-Path $targetRoot "01-Client"
$serverTarget = Join-Path $targetRoot "02-Server"
$databaseTarget = Join-Path $targetRoot "03-Database"
$knownIssuesTarget = Join-Path $targetRoot "04-Known-Issues"

New-Item -ItemType Directory -Force -Path $databaseTarget | Out-Null
New-Item -ItemType Directory -Force -Path $knownIssuesTarget | Out-Null

Copy-Item (Join-Path $workspaceRoot "src\\Game.Client") $clientTarget -Recurse
Copy-Item (Join-Path $workspaceRoot "src\\Game.Server") $serverTarget -Recurse
Copy-Item (Join-Path $workspaceRoot "submission\\03-Database\\*") $databaseTarget -Recurse
Copy-Item (Join-Path $workspaceRoot "submission\\04-Known-Issues\\*") $knownIssuesTarget -Recurse

Get-ChildItem $targetRoot -Recurse -Directory -Force |
    Where-Object { $_.Name -in @("bin", "obj") } |
    Remove-Item -Recurse -Force

Write-Host "Prepared submission folder at: $targetRoot"
