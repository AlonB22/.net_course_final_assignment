param(
    [string]$OutputRoot = "submission-output"
)

$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSEdition -ne "Core") {
    $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -eq $pwsh) {
        throw "PowerShell 7 (pwsh) is required to prepare DACPAC exports."
    }

    & $pwsh.Source -ExecutionPolicy Bypass -File $PSCommandPath -OutputRoot $OutputRoot
    exit $LASTEXITCODE
}

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$targetRoot = Join-Path $workspaceRoot $OutputRoot
$sourceDatabaseRoot = Join-Path $workspaceRoot "submission\03-Database"
$databaseTarget = Join-Path $targetRoot "03-Database"
$knownIssuesTarget = Join-Path $targetRoot "04-Known-Issues"
$zipPath = Join-Path $workspaceRoot "DotNetFinalAssignment-Submission.zip"

function Assert-WorkspaceChild {
    param([string]$Path)

    $workspaceFullPath = [System.IO.Path]::GetFullPath($workspaceRoot)
    $targetFullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $targetFullPath.StartsWith($workspaceFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside workspace: $targetFullPath"
    }
}

function Copy-ProjectFolder {
    param(
        [string]$Source,
        [string]$Destination
    )

    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
}

function Ensure-DeliverableDatabases {
    & dotnet run --project (Join-Path $workspaceRoot "tools\DatabaseBootstrapper\DatabaseBootstrapper.csproj") --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Database bootstrapper failed."
    }
}

function Export-Dacpac {
    param(
        [string]$DatabaseName,
        [string]$TargetFile
    )

    $connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

    & sqlpackage /Action:Extract `
        /SourceConnectionString:$connectionString `
        /TargetFile:$TargetFile `
        /p:ExtractAllTableData=True `
        /p:VerifyExtraction=False `
        /Quiet:True

    if ($LASTEXITCODE -ne 0) {
        throw "sqlpackage failed while exporting $DatabaseName."
    }
}

Assert-WorkspaceChild $targetRoot
Assert-WorkspaceChild $zipPath

& dotnet build (Join-Path $workspaceRoot "DotNetFinalAssignment.sln")
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

Ensure-DeliverableDatabases

Export-Dacpac "DotNetFinalAssignment_GameServer" (Join-Path $sourceDatabaseRoot "DotNetFinalAssignment_GameServer.dacpac")
Export-Dacpac "DotNetFinalAssignment_ClientReplay" (Join-Path $sourceDatabaseRoot "DotNetFinalAssignment_ClientReplay.dacpac")

if (Test-Path -LiteralPath $targetRoot) {
    Remove-Item -LiteralPath $targetRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null
New-Item -ItemType Directory -Force -Path $databaseTarget | Out-Null
New-Item -ItemType Directory -Force -Path $knownIssuesTarget | Out-Null

Copy-ProjectFolder (Join-Path $workspaceRoot "src\Game.Client") (Join-Path $targetRoot "01-Client")
Copy-ProjectFolder (Join-Path $workspaceRoot "src\Game.Server") (Join-Path $targetRoot "02-Server")
Copy-ProjectFolder (Join-Path $workspaceRoot "src\Game.Contracts") (Join-Path $targetRoot "01-Client\Game.Contracts")
Copy-ProjectFolder (Join-Path $workspaceRoot "src\Game.Contracts") (Join-Path $targetRoot "02-Server\Game.Contracts")
Copy-Item -Path (Join-Path $sourceDatabaseRoot "*") -Destination $databaseTarget -Recurse -Force
Copy-Item -Path (Join-Path $workspaceRoot "submission\04-Known-Issues\*") -Destination $knownIssuesTarget -Recurse -Force

$clientProjectFile = Join-Path $targetRoot "01-Client\Game.Client.csproj"
$serverProjectFile = Join-Path $targetRoot "02-Server\Game.Server.csproj"
(Get-Content -LiteralPath $clientProjectFile) -replace "\.\.\\Game.Contracts\\Game.Contracts.csproj", "Game.Contracts\Game.Contracts.csproj" |
    Set-Content -LiteralPath $clientProjectFile
(Get-Content -LiteralPath $serverProjectFile) -replace "\.\.\\Game.Contracts\\Game.Contracts.csproj", "Game.Contracts\Game.Contracts.csproj" |
    Set-Content -LiteralPath $serverProjectFile

$nestedContractsExclusion = @"

  <ItemGroup>
    <Compile Remove="Game.Contracts\**\*.cs" />
    <EmbeddedResource Remove="Game.Contracts\**\*" />
    <None Remove="Game.Contracts\**\*" />
  </ItemGroup>
"@

(Get-Content -LiteralPath $clientProjectFile -Raw) -replace "</Project>", "$nestedContractsExclusion`r`n</Project>" |
    Set-Content -LiteralPath $clientProjectFile
(Get-Content -LiteralPath $serverProjectFile -Raw) -replace "</Project>", "$nestedContractsExclusion`r`n</Project>" |
    Set-Content -LiteralPath $serverProjectFile

Get-ChildItem -LiteralPath $targetRoot -Recurse -Directory -Force |
    Where-Object { $_.Name -in @("bin", "obj") } |
    ForEach-Object {
        Assert-WorkspaceChild $_.FullName
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $targetRoot "*") -DestinationPath $zipPath -Force

Write-Host "Prepared submission folder at: $targetRoot"
Write-Host "Prepared submission zip at: $zipPath"
