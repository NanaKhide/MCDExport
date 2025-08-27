#script args:
# update-repo.ps1 [testing|release] [version]

#validate that "version" can be a valid System.Version
if ($null -eq $args[1] -as [System.Version]) {
    Write-Error "Invalid version: $args[1]"
    exit 1
}

#validate param count and print help if invalid
if ($args.Count -ne 2) {
    Write-Error "Usage: update-repo.ps1 [testing|install] [version]"
    exit 1
}

#validate that "testing" or "install" was passed as the first arg
if ($args[0] -notin "testing", "release") {
    Write-Error "Invalid arg: $args[0]"
    exit 1
}

# Read the contents of "repo.json" as a JSON object
$repoArray = @(Get-Content -Raw -Path "repo.json" | ConvertFrom-Json)

# Find the MCDExport entry
$mcdExport = $repoArray | Where-Object { $_.InternalName -eq "MCDExport" }

if ($null -eq $mcdExport) {
    Write-Error "Could not find MCDExport entry in repo.json"
    exit 1
}

#if arg 1 is "testing" then update "DownloadLinkTesting" and "TestingAssemblyVersion"
if ($args[0] -eq "testing") {
    $mcdExport.DownloadLinkTesting = "https://github.com/NanaKhide/MCDExport/releases/download/v$($args[1])/MCDExport.zip"
    $mcdExport.TestingAssemblyVersion = $args[1]
}

#if arg 1 is "release" then update "DownloadLinkInstall" and "AssemblyVersion"
if ($args[0] -eq "release") {
    $mcdExport.DownloadLinkInstall = "https://github.com/NanaKhide/MCDExport/releases/download/v$($args[1])/MCDExport.zip"
    $mcdExport.DownloadLinkUpdate = "https://github.com/NanaKhide/MCDExport/releases/download/v$($args[1])/MCDExport.zip"
    $mcdExport.DownloadLinkTesting = "https://github.com/NanaKhide/MCDExport/releases/download/v$($args[1])/MCDExport.zip"
    $mcdExport.AssemblyVersion = $args[1]
    $mcdExport.TestingAssemblyVersion = $args[1]
}

# Convert the JSON object back to a JSON string
ConvertTo-Json -Depth 100 -InputObject @($repoArray) | Set-Content -Path "repo.json"